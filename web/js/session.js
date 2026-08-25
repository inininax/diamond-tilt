// Contact model + CPU AI + interactive session — ported from C# ContactResolvers/AiPlayers/MatchPlaySession
(function () {
  const ROOT = typeof window !== 'undefined' ? window : globalThis;
  'use strict';

  const { isStrike } = ROOT.DTRules;

  // ---- TimingContactModel ----
  function drive(speed, angle) {
    const flight = { speedMps: speed, angleDeg: angle, dirDeg: 0 };
    return ROOT.BallFlight.clearsWallForHomerun(flight)
      ? { outcome: 'Homerun', flight }
      : { outcome: 'DeepFly', flight };
  }

  function perfectContact(zone, zoneStrike) {
    if (!zoneStrike) return drive(29, 40);
    if (zone === 4) return drive(40, 33);
    return drive(33, 38);
  }

  function timingEvaluate(pitch, absOffset, perfectBand) {
    const zoneStrike = isStrike(pitch.zone);
    const band = Math.min(Math.max(perfectBand, 0), 1);
    if (absOffset <= band) return perfectContact(pitch.zone, zoneStrike);
    switch (absOffset) {
      case 1: return zoneStrike
        ? { outcome: 'Double', flight: { speedMps: 31, angleDeg: 24, dirDeg: 0 } }
        : { outcome: 'LineSingle', flight: { speedMps: 28, angleDeg: 16, dirDeg: 0 } };
      case 2: return zoneStrike
        ? { outcome: 'Single', flight: { speedMps: 25, angleDeg: 12, dirDeg: 0 } }
        : { outcome: 'Foul', flight: null };
      default: return { outcome: 'Foul', flight: null };
    }
  }

  // ---- CPU AI ----
  function mulberry32(seed) {
    let a = seed >>> 0;
    return function () {
      a |= 0; a = (a + 0x6D2B79F5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  const STRIKE_ZONES_LIST = [1, 3, 4, 5, 7];

  function cpuPitch(state, rng) {
    const mustThrowStrike = state.balls >= 3;
    const zone = mustThrowStrike
      ? STRIKE_ZONES_LIST[Math.floor(rng() * STRIKE_ZONES_LIST.length)]
      : 1 + Math.floor(rng() * 9);
    const speedTier = Math.floor(rng() * 3);
    return { zone, speedTier };
  }

  function gaussianTicks(rng, sigma) {
    let u = 0;
    for (let i = 0; i < 4; i++) u += rng();
    let t = (u - 2.0) * sigma;
    let r = Math.round(t);
    return Math.max(-6, Math.min(6, r));
  }

  function cpuSwing(state, rng, sigma, aggressionBase) {
    let aggression = aggressionBase;
    if (state.strikes >= 2) aggression += 0.35;
    if (state.balls >= 3) aggression -= 0.30;
    aggression = Math.max(0.05, Math.min(1.0, aggression));
    if (rng() >= aggression) return { took: false, offsetTicks: 0 };
    return { took: true, offsetTicks: gaussianTicks(rng, sigma) };
  }

  // ---- Interactive session (port of MatchPlaySession) ----
  const BETWEEN_PLAYS_TICKS = 45;
  const FLIGHT_BY_TIER = { 0: 64, 1: 52, 2: 40 };
  const MAX_OFFSET = 6;

  class MatchPlaySession {
    constructor(seed, rewards) {
      this.rng = mulberry32(seed);
      this.rewards = rewards || null;
      this.engine = new ROOT.DTRules.MatchEngine(
        { evaluate: (pitch, abs, band) => timingEvaluate(pitch, abs, band) },
        this.rng,
      );
      this.tps = 60;
      this.currentTick = 0;
      this.phase = 'WaitingToPitch';
      this.flightTicks = 0;
      this.arrivalTick = -1;
      this.incomingPitch = null;
      this.lastContactFlight = null;
      this.lastOutcome = null;
      this.lastContactWasSwing = false;
      this.lastSwingTick = -1;
      this._resumeTick = 0;
      this._rewardsApplied = false;
      this.summary = null;
    }

    get state() { return this.engine.s; }
    get playerBatting() { return !this.state.isTop; }
    drainEvents() { return this.engine.drainEvents(); }

    tickAdvance(ticks = 1) {
      if (this.phase === 'MatchOver') return;
      for (let i = 0; i < ticks; i++) {
        this.currentTick++;
        if (this.phase === 'BallIncoming' && this.currentTick > this.arrivalTick + MAX_OFFSET) {
          if (this._cpuSwingPending) {
            const pitch = this.incomingPitch;
            const swing = cpuSwing(this.state, this.rng, 1.1, 0.65);
            this.recordContact(pitch, swing);
            this.engine.throwPitch(pitch, swing);
            this._cpuSwingPending = false;
            this.beginBetweenPlays();
            if (this.phase === 'MatchOver') return;
            continue;
          }
          this.resolveTake();
          if (this.phase === 'MatchOver') return;
        } else if (this.phase === 'BetweenPlays' && this.currentTick >= this._resumeTick) {
          this.beginNextPlay();
          if (this.phase === 'MatchOver') return;
        }
      }
    }

    playerPitch(zone, speedTier) {
      if (this.phase !== 'WaitingToPitch' || !this.state.isTop) return false;
      this.incomingPitch = { zone, speedTier };
      this.flightTicks = FLIGHT_BY_TIER[speedTier] || 52;
      this.arrivalTick = this.currentTick + this.flightTicks;
      this.phase = 'BallIncoming';
      this._cpuSwingPending = true;
      return true;
    }

    playerSwing() {
      if (this.phase !== 'BallIncoming') return false;
      if (this._cpuSwingPending) return false;
      if (this.currentTick < this.arrivalTick - Math.floor(this.flightTicks * 0.4)) return false;
      const offset = this.currentTick - this.arrivalTick;
      const swing = { took: true, offsetTicks: Math.max(-MAX_OFFSET, Math.min(MAX_OFFSET, offset)) };
      this.lastSwingTick = this.currentTick;
      this.recordContact(this.incomingPitch, swing);
      this.engine.throwPitch(this.incomingPitch, swing);
      this.beginBetweenPlays();
      return true;
    }

    recordContact(pitch, swing) {
      this.lastContactWasSwing = swing.took;
      const abs = Math.min(Math.abs(swing.offsetTicks), 2147483647);
      const band = (pitch.speedTier || 0) === 0 ? 1 : 0;
      const res = timingEvaluate(pitch, abs, band);
      this.lastOutcome = res.outcome;
      this.lastContactFlight = res.flight;
    }

    resolveTake() {
      this.lastContactWasSwing = false;
      this.lastContactFlight = null;
      this.engine.throwPitch(this.incomingPitch, { took: false, offsetTicks: 0 });
      this.beginBetweenPlays();
    }

    beginBetweenPlays() {
      this.phase = 'BetweenPlays';
      this._resumeTick = this.currentTick + BETWEEN_PLAYS_TICKS;
      if (this.state.phase === 'Finished') this.applyRewardsIfPending();
    }

    beginNextPlay() {
      if (this.state.phase === 'Finished') { this.applyRewardsIfPending(); this.phase = 'MatchOver'; return; }
      if (this.state.isTop) {
        this.phase = 'WaitingToPitch';
      } else {
        this.incomingPitch = cpuPitch(this.state, this.rng);
        this.flightTicks = FLIGHT_BY_TIER[this.incomingPitch.speedTier];
        this.arrivalTick = this.currentTick + this.flightTicks;
        this.phase = 'BallIncoming';
      }
    }

    applyRewardsIfPending() {
      if (this._rewardsApplied || !this.rewards) return;
      this._rewardsApplied = true;
      const stats = { hits: 0, hrs: 0, strikeouts: 0, homeHits: 0, homeHrs: 0 };
      const evts = this.engine.drainEvents();
      for (const e of evts) {
        if (e.type === 'HitRecorded') { stats.hits++; if (!e.isTop) stats.homeHits++; }
        if (e.type === 'HomerunRecorded') { stats.hrs++; stats.hits++; if (!e.isTop) { stats.homeHrs++; stats.homeHits++; } }
        if (e.type === 'BatterStruckOut') stats.strikeouts++;
      }
      this.rewards(this.state.result, stats);
      this.summary = {
        result: this.state.result,
        awayRuns: this.state.awayRuns,
        homeRuns: this.state.homeRuns,
      };
      this.engine.events = evts.concat(this.engine.events);
    }
  }

  ROOT.DTInput = { MAX_OFFSET, cpuPitch, cpuSwing, timingEvaluate };
  ROOT.DTSession = { MatchPlaySession, BETWEEN_PLAYS_TICKS, FLIGHT_BY_TIER };
})();
