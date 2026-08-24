// Baseball rules engine — ported from Assets/Scripts/Core (C#) MatchEngine/BaseRunnerEngine
(function () {
  const ROOT = typeof window !== 'undefined' ? window : globalThis;
  'use strict';

  const STRIKE_ZONES = new Set([1, 3, 4, 5, 7]);
  const BALLS_FOR_WALK = 4;
  const STRIKES_FOR_OUT = 3;
  const INNINGS = 3;

  function isStrike(zone) { return zone >= 1 && zone <= 9 && STRIKE_ZONES.has(zone); }

  function makeState() {
    return {
      inning: 1, isTop: true,
      balls: 0, strikes: 0, outs: 0,
      first: false, second: false, third: false,
      awayRuns: 0, homeRuns: 0,
      phase: 'InProgress', result: null, // 'Away'|'Home'|'Draw'
    };
  }

  function addRun(s, events) {
    if (s.isTop) s.awayRuns++; else s.homeRuns++;
    events.push({ type: 'RunScored', inning: s.inning, isTop: s.isTop });
  }

  function forceAdvanceForWalk(s, events) {
    const f = s.first, sec = s.second, t = s.third;
    if (f && sec && t) addRun(s, events);
    else if (sec && f) { s.third = true; events.push({ type: 'RunnerAdvanced', inning: s.inning, isTop: s.isTop }); }
    else if (f) { s.second = true; events.push({ type: 'RunnerAdvanced', inning: s.inning, isTop: s.isTop }); }
    s.first = true;
  }

  function advanceAllOnHit(s, events, bases) {
    if (bases < 1 || bases > 4) throw new Error('bases out of range');
    let n1 = false, n2 = false, n3 = false, runs = 0;
    const move = (from) => {
      const dest = from + bases;
      if (dest >= 4) runs++;
      else if (dest === 1) n1 = true;
      else if (dest === 2) n2 = true;
      else n3 = true;
    };
    if (s.first) move(1);
    if (s.second) move(2);
    if (s.third) move(3);
    move(0);
    s.first = n1; s.second = n2; s.third = n3;
    for (let i = 0; i < runs; i++) addRun(s, events);
  }

  function resetBatterCount(s) { s.balls = 0; s.strikes = 0; }
  function resetHalfInning(s) {
    s.balls = 0; s.strikes = 0; s.outs = 0;
    s.first = false; s.second = false; s.third = false;
  }

  class MatchEngine {
    constructor(contactResolver, rng) {
      this.resolver = contactResolver;
      this.rng = rng;
      this.s = makeState();
      this.events = [];
    }

    drainEvents() { const e = this.events; this.events = []; return e; }

    throwPitch(pitch, swing) {
      if (!Number.isInteger(pitch.zone) || pitch.zone < 1 || pitch.zone > 9)
        throw new Error('zone out of range');
      const s = this.s;
      if (s.phase === 'Finished') return;

      if (!swing.took) {
        if (isStrike(pitch.zone)) this.addStrike();
        else this.resolveBall();
        return;
      }

      const abs = Math.min(Math.abs(swing.offsetTicks), 2147483647);
      const missThreshold = 4 - (pitch.speedTier || 0);
      const perfectBand = (pitch.speedTier || 0) === 0 ? 1 : 0;

      if (abs >= missThreshold) { this.addStrike(); return; }
      this.resolveContact(pitch, abs, perfectBand);
    }

    resolveBall() {
      const s = this.s;
      s.balls++;
      if (s.balls >= BALLS_FOR_WALK) {
        this.events.push({ type: 'BatterWalked', inning: s.inning, isTop: s.isTop });
        forceAdvanceForWalk(s, this.events);
        resetBatterCount(s);
        this.checkHalfInningEnd();
        this.checkWalkoff();
        return;
      }
      this.events.push({ type: 'BallCalled', inning: s.inning, isTop: s.isTop });
    }

    addStrike() {
      const s = this.s;
      s.strikes++;
      if (s.strikes >= STRIKES_FOR_OUT) { this.recordOut('BatterStruckOut'); return; }
      this.events.push({ type: 'StrikeCalled', inning: s.inning, isTop: s.isTop });
    }

    resolveFoul() {
      const s = this.s;
      if (s.strikes < STRIKES_FOR_OUT - 1) s.strikes++;
      this.events.push({ type: 'StrikeCalled', inning: s.inning, isTop: s.isTop });
    }

    resolveContact(pitch, absOffset, perfectBand) {
      const res = this.resolver.evaluate(pitch, absOffset, perfectBand);
      this.lastOutcome = res.outcome;
      this.lastFlight = res.flight || null;
      switch (res.outcome) {
        case 'Foul': this.resolveFoul(); break;
        case 'Grounder': this.resolveGrounder(); break;
        case 'Homerun':
        case 'DeepFly': if (this.clearsWall(res)) { this.resolveHit(4); break; }
          if (res.outcome === 'DeepFly') { this.resolveDeepFly(); break; }
          this.resolveHit(4); break;
        case 'Single':
        case 'LineSingle': this.resolveHit(1); break;
        case 'Double': this.resolveHit(2); break;
        case 'Triple': this.resolveHit(3); break;
        default: throw new Error('unhandled outcome ' + res.outcome);
      }
    }

    clearsWall(res) {
      if (res.outcome === 'Homerun') return true;
      return !!(res.flight && ROOT.BallFlight.clearsWallForHomerun(res.flight));
    }

    resolveHit(bases) {
      const s = this.s;
      advanceAllOnHit(s, this.events, bases);
      this.events.push({
        type: bases >= 4 ? 'HomerunRecorded' : 'HitRecorded',
        inning: s.inning, isTop: s.isTop,
      });
      resetBatterCount(s);
      this.checkHalfInningEnd();
      this.checkWalkoff();
    }

    resolveGrounder() {
      const s = this.s;
      if (s.first && s.outs < 2) {
        this.recordOut('RunnerOut');
        s.first = false;
        this.recordOut('BatterOut');
        return;
      }
      const runnerScores = s.third && s.outs < 2;
      this.recordOut('BatterOut');
      if (runnerScores && s.outs < 3) {
        s.third = false;
        addRun(s, this.events);
      }
      this.checkHalfInningEnd();
      this.checkWalkoff();
    }

    resolveDeepFly() {
      const s = this.s;
      const tagsUp = s.third && s.outs < 2;
      this.recordOut('BatterOut');
      if (tagsUp && s.outs < 3) {
        s.third = false;
        addRun(s, this.events);
      }
      this.checkHalfInningEnd();
      this.checkWalkoff();
    }

    recordOut(type) {
      const s = this.s;
      s.outs++;
      this.events.push({ type, inning: s.inning, isTop: s.isTop });
      resetBatterCount(s);
      this.checkHalfInningEnd();
    }

    checkHalfInningEnd() {
      const s = this.s;
      if (s.outs < 3 || s.phase === 'Finished') return;

      this.events.push({ type: 'HalfInningEnded', inning: s.inning, isTop: s.isTop });

      if (!s.isTop) {
        if (s.inning >= INNINGS) { this.finishMatch(); return; }
        s.inning++; s.isTop = true;
      } else {
        if (s.inning >= INNINGS && s.homeRuns > s.awayRuns) { this.finishMatch(); return; }
        s.isTop = false;
      }
      resetHalfInning(s);
    }

    checkWalkoff() {
      const s = this.s;
      if (s.phase === 'Finished') return;
      if (s.isTop || s.inning < INNINGS) return;
      if (s.homeRuns > s.awayRuns) this.finishMatch();
    }

    finishMatch() {
      const s = this.s;
      s.phase = 'Finished';
      s.result = s.awayRuns > s.homeRuns ? 'Away' : s.homeRuns > s.awayRuns ? 'Home' : 'Draw';
      this.events.push({ type: 'MatchEnded', inning: s.inning, isTop: s.isTop });
    }
  }

  ROOT.DTRules = {
    INNINGS, BALLS_FOR_WALK, STRIKES_FOR_OUT,
    isStrike, makeState, MatchEngine,
    advanceAllOnHit, forceAdvanceForWalk,
  };
})();
