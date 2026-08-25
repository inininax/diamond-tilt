// Logic tests — ported from C# EditMode suite. Run: node web/tests/rules.test.js
const assert = require('assert');
require('../js/rules.js');
require('../js/ballflight.js');
require('../js/session.js');

const { MatchEngine, makeState, isStrike } = global.DTRules;

function resolver() {
  return { evaluate: (pitch, abs, band) => global.DTInput.timingEvaluate(pitch, abs, band) };
}

function newEngine() { return new MatchEngine(resolver(), Math.random); }
function strikeOut(e) {
  for (let i = 0; i < 3; i++) e.throwPitch({ zone: 4, speedTier: 1 }, { took: false, offsetTicks: 0 });
}

let passed = 0, failed = 0;
function test(name, fn) {
  try { fn(); passed++; console.log('  ok', name); }
  catch (e) { failed++; console.error('  FAIL', name, '\n    ', e.message); }
}

console.log('rules.test.js');

test('ball outside zone counts ball', () => {
  const e = newEngine();
  e.throwPitch({ zone: 9, speedTier: 1 }, { took: false, offsetTicks: 0 });
  assert.equal(e.s.balls, 1);
});

test('four balls walks batter', () => {
  const e = newEngine();
  for (let i = 0; i < 4; i++) e.throwPitch({ zone: 9, speedTier: 1 }, { took: false, offsetTicks: 0 });
  assert.equal(e.s.first, true);
  assert.ok(e.events.some(x => x.type === 'BatterWalked') || e.drainEvents().some(x => x.type === 'BatterWalked'));
});

test('bases-loaded walk forces run in', () => {
  const e = newEngine();
  Object.assign(e.s, { first: true, second: true, third: true });
  for (let i = 0; i < 4; i++) e.throwPitch({ zone: 9, speedTier: 1 }, { took: false, offsetTicks: 0 });
  assert.equal(e.s.awayRuns, 1);
});

test('walk with first+third loads bases, no run', () => {
  const e = newEngine();
  Object.assign(e.s, { first: true, third: true });
  for (let i = 0; i < 4; i++) e.throwPitch({ zone: 9, speedTier: 1 }, { took: false, offsetTicks: 0 });
  assert.equal(e.s.awayRuns, 0);
  assert.ok(e.s.first && e.s.second && e.s.third);
});

test('third strike strikes out batter', () => {
  const e = newEngine();
  strikeOut(e);
  assert.equal(e.s.outs, 1);
});

test('foul with two strikes does not add third', () => {
  const e = newEngine();
  e.throwPitch({ zone: 4, speedTier: 1 }, { took: false, offsetTicks: 0 });
  e.throwPitch({ zone: 4, speedTier: 1 }, { took: false, offsetTicks: 0 });
  e.throwPitch({ zone: 9, speedTier: 1 }, { took: true, offsetTicks: 2 }); // chased ball, offset2 = foul
  assert.equal(e.s.strikes, 2);
});

test('double scores runner from second', () => {
  const e = newEngine();
  e.s.second = true;
  e.throwPitch({ zone: 4, speedTier: 1 }, { took: true, offsetTicks: 1 }); // offset1+zone4 = Double
  assert.equal(e.s.awayRuns, 1);
  assert.equal(e.s.second, true);
});

test('grand slam scores four', () => {
  const e = newEngine();
  Object.assign(e.s, { first: true, second: true, third: true });
  e.throwPitch({ zone: 4, speedTier: 1 }, { took: true, offsetTicks: 0 }); // meat perfect = HR
  assert.equal(e.s.awayRuns, 4);
  assert.ok(!e.s.first && !e.s.second && !e.s.third);
});

test('grounder DP with runner on first (<2 outs): two outs recorded', () => {
  const grounderEngine = new MatchEngine(
    { evaluate: () => ({ outcome: 'Grounder', flight: null }) }, Math.random);
  grounderEngine.s.first = true;
  grounderEngine.throwPitch({ zone: 4, speedTier: 1 }, { took: true, offsetTicks: 2 });
  assert.equal(grounderEngine.s.outs, 2);
  assert.equal(grounderEngine.s.first, false);
});

test('grounder DP at 2 outs does not double-play', () => {
  const grounderEngine = new MatchEngine(
    { evaluate: () => ({ outcome: 'Grounder', flight: null }) }, Math.random);
  grounderEngine.s.first = true;
  grounderEngine.s.outs = 2;
  grounderEngine.throwPitch({ zone: 4, speedTier: 1 }, { took: true, offsetTicks: 2 });
  assert.equal(grounderEngine.s.outs, 0); // third out ends the half, count resets
  assert.ok(grounderEngine.events.some(x => x.type === 'BatterOut'));
  assert.ok(!grounderEngine.events.some(x => x.type === 'RunnerOut')); // no DP at 2 outs
});

test('deep fly as third out: no run counts', () => {
  const e = newEngine();
  e.s.third = true; e.s.outs = 2;
  e.throwPitch({ zone: 1, speedTier: 1 }, { took: true, offsetTicks: 0 }); // corner perfect = DeepFly
  assert.equal(e.s.awayRuns, 0);
  assert.ok(e.drainEvents().some(x => x.type === 'HalfInningEnded'));
});

test('walk-off: sac fly in bottom of final wins it', () => {
  const e = newEngine();
  Object.assign(e.s, { inning: 3, isTop: false, awayRuns: 1, homeRuns: 1, third: true });
  e.throwPitch({ zone: 1, speedTier: 1 }, { took: true, offsetTicks: 0 }); // corner perfect = sac fly
  assert.equal(e.s.phase, 'Finished');
  assert.equal(e.s.result, 'Home');
  assert.equal(e.s.homeRuns, 2);
});

test('final inning top half, home ahead: bottom skipped', () => {
  const e = newEngine();
  Object.assign(e.s, { inning: 3, isTop: true, awayRuns: 1, homeRuns: 2 });
  strikeOut(e); strikeOut(e); strikeOut(e);
  assert.equal(e.s.phase, 'Finished');
  assert.equal(e.s.result, 'Home');
});

test('full match: 18 Ks ends in draw', () => {
  const e = newEngine();
  for (let i = 0; i < 18; i++) strikeOut(e);
  assert.equal(e.s.phase, 'Finished');
  assert.equal(e.s.result, 'Draw');
});

test('ballflight: sample trajectory sane', () => {
  const pts = global.BallFlight.sample({ speedMps: 38, angleDeg: 30, dirDeg: 0 }, 20);
  assert.equal(pts.length, 20);
  assert.ok(Math.abs(pts[0].y - global.BallFlight.CONTACT_HEIGHT) < 1e-9);
  assert.ok(pts[pts.length - 1].y < 0.5);
  for (let i = 1; i < pts.length; i++) assert.ok(pts[i].z > pts[i - 1].z);
});

test('ballflight: wall clearing thresholds', () => {
  assert.equal(global.BallFlight.clearsWallForHomerun({ speedMps: 25, angleDeg: 40, dirDeg: 0 }), false);
  assert.equal(global.BallFlight.clearsWallForHomerun({ speedMps: 48, angleDeg: 32, dirDeg: 0 }), true);
  assert.equal(global.BallFlight.clearsWallForHomerun({ speedMps: 50, angleDeg: 32, dirDeg: 80 }), false);
});

test('session: full interactive match completes (perfect player policy, mixed zones)', () => {
  const s = new global.DTSession.MatchPlaySession(777);
  let guard = 60000;
  const zoneRng = mulberry32(888);
  while (s.phase !== 'MatchOver' && guard-- > 0) {
    if (s.phase === 'WaitingToPitch') s.playerPitch(1 + Math.floor(zoneRng() * 9), Math.floor(zoneRng() * 3));
    else if (s.phase === 'BallIncoming' && s.currentTick >= s.arrivalTick) s.playerSwing();
    s.tickAdvance(1);
  }
  assert.equal(s.phase, 'MatchOver');
  assert.equal(s.state.phase, 'Finished');
});

test('session: rewards applied once', () => {
  let calls = 0;
  const s = new global.DTSession.MatchPlaySession(42, () => calls++);
  const zoneRng = mulberry32(999);
  let guard = 60000;
  while (s.phase !== 'MatchOver' && guard-- > 0) {
    if (s.phase === 'WaitingToPitch') s.playerPitch(1 + Math.floor(zoneRng() * 9), Math.floor(zoneRng() * 3));
    else if (s.phase === 'BallIncoming' && s.currentTick >= s.arrivalTick) s.playerSwing();
    s.tickAdvance(1);
  }
  assert.equal(calls, 1);
});

console.log(`\n${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);

function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
