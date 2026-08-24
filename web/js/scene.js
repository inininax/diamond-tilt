// 3D stadium + articulated humanoid characters (three.js r128)
(function () {
  'use strict';

  const FIELD_GREEN = 0x1d6b2a;
  const DIRT = 0x9a7448;
  const LINE_WHITE = 0xf5f5f5;

  function mat(color, opts) {
    return new THREE.MeshLambertMaterial(Object.assign({ color }, opts || {}));
  }

  function box(w, h, d, color) {
    return new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat(color));
  }

  function cyl(rt, rb, h, color, seg) {
    return new THREE.Mesh(new THREE.CylinderGeometry(rt, rb, h, seg || 16), mat(color));
  }

  function sph(r, color, seg) {
    return new THREE.Mesh(new THREE.SphereGeometry(r, seg || 14, seg || 12), mat(color));
  }

  function limb(len, r, color) {
    const pivot = new THREE.Group();
    const mesh = cyl(r, r * 0.85, len, color);
    mesh.position.y = -len / 2;
    pivot.add(mesh);
    return pivot;
  }

  // Articulated humanoid: pelvis → torso → head; shoulder/elbow, hip/knee joints
  function makeHumanoid(uniform, skin, opts) {
    opts = opts || {};
    const root = new THREE.Group();
    const body = new THREE.Group();
    root.add(body);

    const legL = limb(0.48, 0.075, 0xf2f2f2);
    legL.position.set(-0.12, 0.95, 0);
    const legR = limb(0.48, 0.075, 0xf2f2f2);
    legR.position.set(0.12, 0.95, 0);
    body.add(legL, legR);

    const pelvis = box(0.34, 0.22, 0.24, 0xdedede);
    pelvis.position.y = 0.98;
    body.add(pelvis);

    const torso = box(0.42, 0.55, 0.28, uniform);
    torso.position.y = 1.36;
    body.add(torso);

    const head = sph(0.155, skin);
    head.position.y = 1.82;
    body.add(head);
    const cap = cyl(0.16, 0.16, 0.09, uniform);
    cap.position.y = 1.92;
    body.add(cap);
    const brim = box(0.2, 0.03, 0.14, uniform);
    brim.position.set(0, 1.89, -0.16);
    body.add(brim);

    const armL = limb(0.52, 0.055, uniform);
    armL.position.set(-0.28, 1.58, 0);
    const armR = limb(0.52, 0.055, uniform);
    armR.position.set(0.28, 1.58, 0);
    body.add(armL, armR);

    const figure = {
      root, body, head, torso,
      armL, armR, legL, legR,
      bat: null,
      swingT: -1,
      runPhase: Math.random() * Math.PI * 2,
      baseY: 0,
    };

    if (opts.bat) {
      const batPivot = new THREE.Group();
      batPivot.position.set(0.3, 1.5, 0.05);
      const batMesh = cyl(0.035, 0.05, 0.85, 0xd8a35a);
      batMesh.position.y = -0.42;
      batPivot.add(batMesh);
      batPivot.rotation.x = 0.4;
      body.add(batPivot);
      figure.bat = batPivot;
    }
    return figure;
  }

  function setShadows(mesh, cast, receive) {
    mesh.castShadow = !!cast;
    mesh.receiveShadow = !!receive;
    mesh.traverse && mesh.traverse(o => { if (o.isMesh) { o.castShadow = !!cast; o.receiveShadow = !!receive; } });
  }

  class Stadium {
    constructor(scene) {
      this.scene = scene;
      this.root = new THREE.Group();
      scene.add(this.root);
      this.players = {};
      this.runners = [null, null, null];
      this.ball = null;
      this.trailPts = [];
      this.build();
    }

    build() {
      const R = this.root;

      const grass = new THREE.Mesh(new THREE.PlaneGeometry(300, 300), mat(FIELD_GREEN));
      grass.rotation.x = -Math.PI / 2;
      grass.position.set(0, 0, 50);
      grass.receiveShadow = true;
      R.add(grass);

      for (let i = 0; i < 8; i++) {
        if (i % 2) continue;
        const stripe = new THREE.Mesh(new THREE.PlaneGeometry(300, 12), mat(0x1a5f24));
        stripe.rotation.x = -Math.PI / 2;
        stripe.position.set(0, 0.01, 18 + i * 12);
        R.add(stripe);
      }

      const dirt = new THREE.Mesh(new THREE.PlaneGeometry(46, 46), mat(DIRT));
      dirt.rotation.x = -Math.PI / 2;
      dirt.rotation.z = Math.PI / 4;
      dirt.position.set(0, 0.012, 16);
      dirt.receiveShadow = true;
      R.add(dirt);

      const mound = cyl(2.6, 2.8, 0.3, 0xa87f52);
      mound.position.set(0, 0.15, 18.4);
      mound.castShadow = true;
      R.add(mound);

      const plate = box(0.75, 0.1, 0.75, LINE_WHITE);
      plate.position.set(0, 0.05, 0);
      R.add(plate);

      const basePos = [[19.4, 19.4], [0, 38.8], [-19.4, 19.4]];
      basePos.forEach(([x, z], i) => {
        const b = box(0.75, 0.1, 0.75, LINE_WHITE);
        b.position.set(x, 0.05, z);
        R.add(b);
        this['baseMarker' + i] = b;
      });

      [[1.6, 1.6, 72, 72], [-1.6, 1.6, -72, 72]].forEach(([x0, z0, x1, z1]) => {
        const len = Math.hypot(x1 - x0, z1 - z0);
        const line = box(0.3, 0.03, len, LINE_WHITE);
        line.position.set((x0 + x1) / 2, 0.03, (z0 + z1) / 2);
        line.rotation.y = Math.atan2(x1 - x0, z1 - z0);
        R.add(line);
      });

      const wallMat = mat(0x20304f);
      const SEGS = 24;
      for (let i = 0; i < SEGS; i++) {
        const a0 = THREE.MathUtils.lerp(-52, 52, i / SEGS) * Math.PI / 180;
        const a1 = THREE.MathUtils.lerp(-52, 52, (i + 1) / SEGS) * Math.PI / 180;
        const p0 = new THREE.Vector3(Math.sin(a0) * 100, 0, Math.cos(a0) * 100);
        const p1 = new THREE.Vector3(Math.sin(a1) * 100, 0, Math.cos(a1) * 100);
        const mid = p0.clone().add(p1).multiplyScalar(0.5);
        const seg = box(p0.distanceTo(p1) + 0.4, 3.2, 1.2, 0x20304f);
        seg.position.set(mid.x, 1.6, mid.z);
        seg.rotation.y = Math.atan2(p1.x - p0.x, p1.z - p0.z);
        R.add(seg);
      }
      const topLine = new THREE.Mesh(
        new THREE.TorusGeometry(100, 0.09, 6, 48, THREE.MathUtils.degToRad(104)),
        mat(0xf7d247));
      topLine.position.set(0, 3.25, 0);
      topLine.rotation.y = Math.PI / 2 + THREE.MathUtils.degToRad(-38);
      R.add(topLine);

      const crowdColors = [0xe8b4b8, 0xa8c8e8, 0xf0e0a0, 0xb8e0b8, 0xe8a0a0, 0xc0b0e8];
      const crowdGeo = new THREE.BoxGeometry(0.55, 0.9, 0.5);
      const crowdMat = mat(0xffffff);
      const COUNT = 420;
      const crowd = new THREE.InstancedMesh(crowdGeo, crowdMat, COUNT);
      const dummy = new THREE.Object3D();
      const color = new THREE.Color();
      let idx = 0;
      for (let tier = 0; tier < 3 && idx < COUNT; tier++) {
        const radius = 107 + tier * 7;
        const y = 2.4 + tier * 2.2;
        for (let i = 0; i < Math.floor(COUNT / 3) && idx < COUNT; i++, idx++) {
          const a = THREE.MathUtils.lerp(-54, 54, i / Math.floor(COUNT / 3)) * Math.PI / 180;
          dummy.position.set(Math.sin(a) * radius, y, Math.cos(a) * radius);
          dummy.rotation.y = a + Math.PI;
          dummy.updateMatrix();
          crowd.setMatrixAt(idx, dummy.matrix);
          crowd.setColorAt(idx, color.setHex(crowdColors[(idx * 7) % crowdColors.length]));
        }
      }
      R.add(crowd);

      [[-45, 45], [45, 45], [-45, 95], [45, 95]].forEach(([x, z]) => {
        const px = Math.sin(x * Math.PI / 180) * 118;
        const pz = Math.cos(z * Math.PI / 180) * 118;
        const pole = cyl(0.28, 0.36, 26, 0x384048);
        pole.position.set(Math.sin(THREE.MathUtils.degToRad(x)) * 118, 13, Math.cos(THREE.MathUtils.degToRad(x * 0.5 + 30)) * 60 + 60);
        R.add(pole);
        const panel = box(3.4, 1.6, 0.5, 0xfff7cc);
        panel.position.set(pole.position.x, 26, pole.position.z);
        panel.lookAt(0, 2, 30);
        R.add(panel);
        const glow = new THREE.PointLight(0xfff2cc, 0.35, 160);
        glow.position.copy(panel.position);
        R.add(glow);
      });

      const foulPoleL = cyl(0.12, 0.16, 14, 0xf7d247);
      foulPoleL.position.set(-Math.sin(THREE.MathUtils.degToRad(45)) * 100, 7, Math.cos(THREE.MathUtils.degToRad(45)) * 100);
      R.add(foulPoleL);
      const foulPoleR = foulPoleL.clone();
      foulPoleR.position.x *= -1;
      R.add(foulPoleR);

      this.buildBall();
      this.buildPlayers();
    }

    buildBall() {
      const ball = sph(0.16, 0xffffff, 10);
      ball.castShadow = true;
      this.root.add(ball);
      this.ball = ball;
      this.ball.visible = false;

      const trailGeo = new THREE.BufferGeometry();
      const MAX = 60;
      this.trailPts = [];
      this.trailMax = MAX;
      this.trailLine = new THREE.Line(trailGeo, new THREE.LineBasicMaterial({
        color: 0xffffff, transparent: true, opacity: 0.65,
      }));
      this.root.add(this.trailLine);
    }

    buildPlayers() {
      const defense = 0x2a3f7d;
      const attack = 0xd63c34;
      const skin = 0xd9a06b;

      this.players.pitcher = makeHumanoid(defense, skin);
      this.players.pitcher.root.position.set(0, 0.3, 18.0);
      this.root.add(this.players.pitcher.root);

      this.players.catcher = makeHumanoid(0x333a45, skin);
      this.players.catcher.root.position.set(0, 0, -1.7);
      this.players.catcher.root.rotation.y = Math.PI;
      this.root.add(this.players.catcher.root);

      const fielders = [
        ['1b', 24, 26], ['2b', 8, 33], ['ss', -8, 33], ['3b', -24, 26],
        ['lf', -34, 64], ['cf', 0, 74], ['rf', 34, 64],
      ];
      fielders.forEach(([name, x, z]) => {
        const f = makeHumanoid(defense, skin);
        f.root.position.set(x, 0, z);
        f.root.rotation.y = Math.atan2(-x, -z) + Math.PI;
        this.root.add(f.root);
        this.players[name] = f;
      });

      this.players.batter = makeHumanoid(attack, skin, { bat: true });
      this.players.batter.root.position.set(0.85, 0, 0.3);
      this.players.batter.root.rotation.y = Math.PI + 0.15;
      this.root.add(this.players.batter.root);

      this.players.umpire = makeHumanoid(0x14161c, skin);
      this.players.umpire.root.position.set(-0.7, 0, -2.0);
      this.players.umpire.root.rotation.y = Math.PI;
      this.root.add(this.players.umpire.root);
    }

    setRunnerBase(index, baseVec) {
      if (!this.runners[index]) {
        this.runners[index] = makeHumanoid(0xd63c34, 0xd9a06b);
        this.root.add(this.runners[index].root);
      }
      const r = this.runners[index];
      r.root.visible = true;
      r.baseTarget = baseVec;
      r.root.position.set(baseVec.x + 0.4, 0, baseVec.z - 0.4);
      r.runT = 0;
    }

    hideRunner(index) {
      if (this.runners[index]) this.runners[index].root.visible = false;
    }

    updateRunnersFromState(state, outcome, animate) {
      const now = [state.first, state.second, state.third];
      const pos = [new THREE.Vector3(19.4, 0, 19.4), new THREE.Vector3(0, 0, 38.8), new THREE.Vector3(-19.4, 0, 19.4)];
      for (let i = 0; i < 3; i++) {
        if (now[i]) this.setRunnerBase(i, pos[i]);
        else if (!animate || !now[i]) this.hideRunner(i);
      }
    }

    startPitchAnimation(durationSec) {
      this.pitchT = 0;
      this.pitchDur = Math.max(0.25, durationSec);
      this.pitchFlying = true;
      this.ball.visible = true;
      this.ball.position.set(0, 1.85, 17.9);
      if (this.trailLine) this.trailPts.length = 0;
      this.pitcherAnim = { t: 0 };
    }

    startContactAnimation(flight, origin) {
      this.pitchFlying = false;
      const pts = window.BallFlight.sample(flight, 30);
      this.contactPts = pts.map(p => new THREE.Vector3(p.x + (origin ? origin.x : 0.2), Math.max(0.08, p.y + (origin ? origin.y : 1.0)), p.z + (origin ? origin.z : 0.4)));
      this.contactT = 0;
      this.contactDur = Math.max(0.9, window.BallFlight.flightTimeNoDrag(flight));
      this.contactFlying = true;
      this.ball.visible = true;
      this.ball.position.copy(this.contactPts[0]);
      if (this.trailLine) this.trailPts.length = 0;
      if (this.players.batter) this.players.batter.swingT = 0;
    }

    ballToCatcher() {
      this.pitchFlying = true;
    }

    hideBall() { this.ball.visible = false; }

    update(dt, session) {
      if (this.pitcherAnim) {
        this.pitcherAnim.t += dt;
        const k = Math.min(1, this.pitcherAnim.t / this.pitchDur);
        const arm = this.players.pitcher.armR;
        arm.rotation.x = THREE.MathUtils.lerp(-2.7, 1.2, Math.pow(k, 0.7));
        this.players.pitcher.body.rotation.y = Math.sin(k * Math.PI) * 0.5;
        if (k >= 1) { this.pitcherAnim = null; arm.rotation.x = 0; this.players.pitcher.body.rotation.y = 0; }
      }

      if (this.pitchFlying) {
        this.pitchT += dt / this.pitchDur;
        const t = Math.min(1, this.pitchT);
        const from = new THREE.Vector3(0, 1.85, 17.9);
        const to = new THREE.Vector3(0.2, 1.0, 0.4);
        this.ball.position.lerpVectors(from, to, t);
        this.ball.position.y += Math.sin(t * Math.PI) * 0.3;
        this.pushTrail();
      }

      if (this.contactFlying && this.contactPts) {
        this.contactT += dt / this.contactDur;
        const t = Math.min(1, this.contactT);
        const f = t * (this.contactPts.length - 1);
        const i0 = Math.floor(f), i1 = Math.min(i0 + 1, this.contactPts.length - 1);
        this.ball.position.lerpVectors(this.contactPts[i0], this.contactPts[i1], f - i0);
        this.pushTrail();
        if (t >= 1) this.contactFlying = false;
      }

      if (this.trailLine && this.trailPts.length > 1) {
        this.trailLine.geometry.setFromPoints(this.trailPts);
        this.trailLine.geometry.setDrawRange(0, this.trailPts.length);
      }

      const t = performance.now() / 1000;
      const swingAnim = (fig) => {
        if (fig.swingT < 0) return;
        fig.swingT += dt;
        const k = Math.min(1, fig.swingT / 0.18);
        if (fig.bat) {
          fig.bat.rotation.z = THREE.MathUtils.lerp(2.4, -1.6, Math.sin(k * Math.PI));
          fig.bat.rotation.x = 0.4;
        }
        fig.armR.rotation.x = THREE.MathUtils.lerp(-0.4, -2.2, Math.sin(k * Math.PI));
        if (fig.swingT > 0.4) { fig.swingT = -1; fig.armR.rotation.x = -0.4; if (fig.bat) fig.bat.rotation.z = 2.4; }
      };
      swingAnim(this.players.batter);

      const breathe = Math.sin(t * 2.2) * 0.012;
      Object.values(this.players).forEach(p => { p.body.position.y = breathe; });
      this.players.batter.body.rotation.y = Math.sin(t * 1.6) * 0.06;

      const runAnim = (fig) => {
        if (!fig.root.visible || !fig.runT && fig.runT !== 0) return;
      };
      [this.runners[0], this.runners[1], this.runners[2]].forEach(r => {
        if (!r || !r.root.visible) return;
        if (r.running !== false) {
          r.runT = (r.runT || 0);
        }
        const moving = r.moving;
        const ph = t * 11;
        r.legL.rotation.x = moving ? Math.sin(ph) * 0.9 : 0;
        r.legR.rotation.x = moving ? -Math.sin(ph) * 0.9 : 0;
        r.armL.rotation.x = moving ? -Math.sin(ph) * 0.7 : 0;
        r.armR.rotation.x = moving ? Math.sin(ph) * 0.7 : 0;
        r.body.position.y = moving ? Math.abs(Math.sin(ph)) * 0.05 : breathe;
      });
    }

    pushTrail() {
      this.trailPts.push(this.ball.position.clone());
      if (this.trailPts.length > this.trailMax) this.trailPts.shift();
    }
  }

  window.DTScene = { Stadium, makeHumanoid };
})();
