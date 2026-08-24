// Ball flight physics — ported from Assets/Scripts/Core/BallFlight.cs
(function () {
  const ROOT = typeof window !== 'undefined' ? window : globalThis;
  'use strict';

  const G = 9.81;
  const WALL_DISTANCE = 100.0;
  const WALL_HEIGHT = 3.0;
  const FOUL_ANGLE = 45.0;
  const DRAG_K = 0.003;
  const HZ = 240;
  const CONTACT_HEIGHT = 1.0;

  function initialVelocity(p) {
    const a = p.angleDeg * Math.PI / 180;
    const d = p.dirDeg * Math.PI / 180;
    const h = p.speedMps * Math.cos(a);
    return { x: h * Math.sin(d), y: p.speedMps * Math.sin(a), z: h * Math.cos(d) };
  }

  function positionNoDrag(p, t) {
    const v = initialVelocity(p);
    return {
      x: v.x * t,
      y: CONTACT_HEIGHT + v.y * t - 0.5 * G * t * t,
      z: v.z * t,
    };
  }

  function flightTimeNoDrag(p) {
    const vy = initialVelocity(p).y;
    const disc = vy * vy + 2 * G * CONTACT_HEIGHT;
    if (disc < 0) return 0;
    return (vy + Math.sqrt(disc)) / G;
  }

  function integrateWithDrag(p) {
    const dt = 1.0 / HZ;
    let pos = { x: 0, y: CONTACT_HEIGHT, z: 0 };
    let vel = initialVelocity(p);
    let apex = pos.y, time = 0, crossedWall = false;

    const step = (v, d) => {
      const speed = Math.hypot(v.x, v.y, v.z);
      const f = Math.max(0, 1 - DRAG_K * speed * dt);
      return { x: v.x * f, y: v.y * f - G * dt, z: v.z * f };
    };
    const hDist = (q) => Math.hypot(q.x, q.z);

    while (pos.y > 0 && time < 30) {
      const next = { x: pos.x + vel.x * dt, y: pos.y + vel.y * dt, z: pos.z + vel.z * dt };
      const nextVel = step(vel, dt);
      time += dt;
      if (next.y > apex) apex = next.y;

      if (!crossedWall && hDist(pos) < WALL_DISTANCE && hDist(next) >= WALL_DISTANCE) {
        const frac = (WALL_DISTANCE - hDist(pos)) / (hDist(next) - hDist(pos));
        crossedWall = (pos.y + (next.y - pos.y) * frac) >= WALL_HEIGHT;
      }

      if (next.y <= 0) {
        const frac = pos.y / (pos.y - next.y);
        const landing = {
          x: pos.x + (next.x - pos.x) * frac,
          y: 0,
          z: pos.z + (next.z - pos.z) * frac,
        };
        return { landing, flightTime: time - dt + dt * frac, apex, distance: Math.hypot(landing.x, landing.z), crossedWall };
      }
      pos = next; vel = nextVel;
    }
    return { landing: pos, flightTime: time, apex, distance: hDist(pos), crossedWall };
  }

  function clearsWallForHomerun(p) {
    if (Math.abs(p.dirDeg) > FOUL_ANGLE) return false;
    return integrateWithDrag(p).crossedWall;
  }

  function sample(p, steps) {
    if (steps < 2) throw new Error('steps < 2');
    const total = flightTimeNoDrag(p);
    const out = [];
    for (let i = 0; i < steps; i++) out.push(positionNoDrag(p, total * i / (steps - 1)));
    return out;
  }

  ROOT.BallFlight = {
    G, WALL_DISTANCE, WALL_HEIGHT, FOUL_ANGLE, CONTACT_HEIGHT,
    initialVelocity, positionNoDrag, flightTimeNoDrag,
    integrateWithDrag, clearsWallForHomerun, sample,
  };
})();
