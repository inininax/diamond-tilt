// 3D stadium + articulated humanoid characters (three.js r128)
// Dimensions follow KBO/공인 규격: 홈-투수 18.44m, 베이스 27.432m, 홈-2루 38.8m,
// LF/RF 99m, CF 122m, 경고 트랙, 배터스아이, 대기타자 서클, 더그아웃, 2층 내야 관중석.
(function () {
  'use strict';

  const FIELD_GREEN = 0x2a7a33;
  const FIELD_GREEN_DARK = 0x236a2b;
  const DIRT = 0xa5794b;
  const DIRT_DARK = 0x8f683e;
  const LINE_WHITE = 0xf5f5f5;

  function mat(color, opts) {
    return new THREE.MeshStandardMaterial(Object.assign({
      color, roughness: 0.88, metalness: 0.02,
    }, opts || {}));
  }

  function box(w, h, d, color, opts) { return new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat(color, opts)); }
  function cyl(rt, rb, h, color, seg) { return new THREE.Mesh(new THREE.CylinderGeometry(rt, rb, h, seg || 18), mat(color, seg ? null : null)); }
  function sph(r, color, seg) { return new THREE.Mesh(new THREE.SphereGeometry(r, seg || 16, seg || 12), mat(color)); }

  function limb(len, r0, r1, color) {
    const pivot = new THREE.Group();
    const mesh = new THREE.Mesh(new THREE.CylinderGeometry(r0, r1, len, 12), mat(color));
    mesh.position.y = -len / 2;
    pivot.add(mesh);
    return pivot;
  }

  function makeCanvasTexture(w, h, draw) {
    const canvas = document.createElement('canvas');
    canvas.width = w; canvas.height = h;
    draw(canvas.getContext('2d'), w, h);
    const tex = new THREE.CanvasTexture(canvas);
    tex.anisotropy = 4;
    return tex;
  }

  function fieldTexture() {
    return makeCanvasTexture(1024, 1024, (ctx, w, h) => {
      ctx.fillStyle = '#2a7a33';
      ctx.fillRect(0, 0, w, h);
      const stripes = 14;
      for (let i = 0; i < stripes; i++) {
        if (i % 2) continue;
        ctx.fillStyle = 'rgba(255,255,255,0.045)';
        ctx.fillRect(0, (i / stripes) * h, w, h / stripes);
      }
      for (let i = 0; i < 2600; i++) {
        ctx.fillStyle = `rgba(${20 + Math.random() * 40 | 0},${90 + Math.random() * 60 | 0},${30 + Math.random() * 30 | 0},0.25)`;
        ctx.fillRect(Math.random() * w, Math.random() * h, 2.5, 2.5);
      }
    });
  }

  function dirtTexture() {
    return makeCanvasTexture(512, 512, (ctx, w, h) => {
      ctx.fillStyle = '#a5794b';
      ctx.fillRect(0, 0, w, h);
      for (let i = 0; i < 3200; i++) {
        const g = 120 + Math.random() * 70 | 0;
        ctx.fillStyle = `rgba(${g + 40},${g - 10},${g - 55},0.3)`;
        ctx.fillRect(Math.random() * w, Math.random() * h, 2, 2);
      }
    });
  }

  function numberTexture(num, bg, fg) {
    return makeCanvasTexture(128, 128, (ctx) => {
      ctx.fillStyle = bg;
      ctx.fillRect(0, 0, 128, 128);
      ctx.fillStyle = fg;
      ctx.font = 'bold 84px sans-serif';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(num, 64, 70);
    });
  }

  function makeHumanoid(opts) {
    opts = opts || {};
    const H = opts.height || 1.8;
    const k = H / 1.8;
    const uniform = opts.uniform != null ? opts.uniform : 0x2a3f7d;
    const pantsC = opts.pants != null ? opts.pants : 0xf2f2f2;
    const skin = opts.skin != null ? opts.skin : 0xd9a06b;
    const root = new THREE.Group();
    const body = new THREE.Group();
    root.add(body);

    const torsoPts = [];
    for (let i = 0; i <= 8; i++) {
      const t = i / 8;
      const y = 1.02 + t * 0.52;
      const r = (0.17 + t * 0.05) * (1 - Math.pow(t, 6) * 0.12) * (t > 0.85 ? 1.12 : 1);
      torsoPts.push(new THREE.Vector2(r * k, y * k));
    }
    const torso = new THREE.Mesh(new THREE.LatheGeometry(torsoPts, 14), mat(uniform, { roughness: 0.92 }));
    torso.castShadow = true;
    body.add(torso);

    const hips = cyl(0.17 * k, 0.155 * k, 0.2 * k, pantsC);
    hips.position.y = 0.94 * k;
    hips.castShadow = true;
    body.add(hips);

    const head = sph(0.115 * k, skin);
    head.position.y = 1.7 * k;
    head.castShadow = true;
    body.add(head);

    let headGear = null;
    if (opts.helmet) {
      headGear = sph(0.135 * k, uniform, 14);
      headGear.scale.set(1, 0.82, 1.08);
      headGear.position.y = 1.75 * k;
      body.add(headGear);
      const brim = cyl(0.14 * k, 0.14 * k, 0.02 * k, uniform);
      brim.position.set(0, 1.7 * k, -0.12 * k);
      body.add(brim);
    } else if (opts.cap !== false) {
      headGear = cyl(0.125 * k, 0.13 * k, 0.07 * k, uniform);
      headGear.position.y = 1.79 * k;
      body.add(headGear);
      const brim = box(0.18 * k, 0.025 * k, 0.13 * k, uniform);
      brim.position.set(0, 1.77 * k, -0.14 * k);
      body.add(brim);
    }
    if (opts.mask) {
      const m = box(0.2 * k, 0.22 * k, 0.1 * k, 0x555f66);
      m.position.set(0, 1.68 * k, -0.1 * k);
      body.add(m);
    }
    if (opts.chestProtector) {
      const cp = box(0.36 * k, 0.42 * k, 0.1 * k, 0x8a2be2);
      cp.position.set(0, 1.32 * k, -0.16 * k);
      body.add(cp);
    }

    const legL = limb(0.5 * k, 0.07 * k, 0.055 * k, pantsC);
    legL.position.set(-0.11 * k, 0.94 * k, 0);
    const legR = limb(0.5 * k, 0.07 * k, 0.055 * k, pantsC);
    legR.position.set(0.11 * k, 0.94 * k, 0);
    body.add(legL, legR);
    const sockL = cyl(0.058 * k, 0.05 * k, 0.14 * k, uniform);
    sockL.position.y = -0.42 * k; legL.add(sockL);
    const sockR = sockL.clone(); legR.add(sockR);

    const armL = limb(0.56 * k, 0.05 * k, 0.042 * k, uniform);
    armL.position.set(-0.24 * k, 1.5 * k, 0);
    const armR = limb(0.56 * k, 0.05 * k, 0.042 * k, uniform);
    armR.position.set(0.24 * k, 1.5 * k, 0);
    body.add(armL, armR);

    const handL = sph(0.05 * k, skin); handL.position.y = -0.58 * k; armL.add(handL);
    const handR = sph(0.05 * k, skin); handR.position.y = -0.58 * k; armR.add(handR);

    if (opts.glove === 'catcher') {
      const mitt = sph(0.1 * k, 0x7a3b2e, 10);
      mitt.scale.set(1, 1.2, 0.7);
      mitt.position.set(0, -0.66 * k, -0.04 * k);
      armL.add(mitt);
    } else if (opts.glove === 'field') {
      const glove = sph(0.085 * k, 0x6b4226, 10);
      glove.scale.set(1, 1.1, 0.55);
      glove.position.set(0, -0.64 * k, -0.05 * k);
      armL.add(glove);
    }

    if (opts.bat) {
      const pts = [];
      for (let i = 0; i <= 6; i++) pts.push(new THREE.Vector2(0.02 + i * 0.008, i * 0.14));
      pts.push(new THREE.Vector2(0.062, 0.88));
      const bat = new THREE.Mesh(new THREE.LatheGeometry(pts, 10), mat(0xd8a35a, { roughness: 0.55 }));
      const batPivot = new THREE.Group();
      batPivot.position.set(0.3 * k, 1.46 * k, 0.06 * k);
      bat.add(batMesh(bat));
      bat.rotation.x = 0.35;
      bat.position.y = -0.1 * k;
      batPivot.add(bat);
      body.add(batPivot);
    }

    if (opts.number != null) {
      const num = new THREE.Mesh(
        new THREE.PlaneGeometry(0.26 * k, 0.26 * k),
        new THREE.MeshBasicMaterial({ map: numberTexture(opts.number, '#' + new THREE.Color(uniform).getHexString(), '#ffffff'), transparent: false }));
      num.position.set(0, 1.38 * k, 0.15 * k);
      body.add(num);
    }

    function batMesh(b) { return b; }

    const figure = {
      root, body, head, torso, armL, armR, legL, legR,
      batPivot: opts.bat ? root.getObjectByName('bat') || null : null,
      bat: root.children.find ? null : null,
      swingT: -1,
      baseY: 0,
    };
    if (opts.bat) {
      figure.batPivot = body.children.filter(c => c.type === 'Group').pop() || null;
      figure.swingAnim = (dt) => {
        if (figure.swingT < 0) return;
        figure.swingT += dt;
        const kk = Math.min(1, figure.swingT / 0.18);
        if (figure.batPivot) {
          figure.batPivot.rotation.z = THREE.MathUtils.lerp(2.2, -1.8, Math.sin(kk * Math.PI));
          figure.batPivot.rotation.x = THREE.MathUtils.lerp(0.2, -0.6, Math.sin(kk * Math.PI));
        }
        figure.armR.rotation.x = THREE.MathUtils.lerp(-0.5, -2.3, Math.sin(kk * Math.PI));
        if (figure.swingT > 0.42) {
          figure.swingT = -1;
          if (figure.batPivot) { figure.batPivot.rotation.set(0.35, 0, 0); }
          figure.armR.rotation.x = -0.5;
        }
      };
    }
    return figure;
  }

  class Stadium {
    constructor(scene) {
      this.scene = scene;
      this.root = new THREE.Group();
      scene.add(this.root);
      this.players = {};
      this.runners = [null, null, null];
      this.trailPts = [];
      this.trailMax = 70;
      this.build();
    }

    build() {
      const R = this.root;
      this.buildField();
      this.buildWall();
      this.buildStands();
      this.buildLightTowers();
      this.buildScoreboard();
      this.buildDugouts();
      this.buildBackstop();
      this.buildBall();
      this.buildPlayers();
      this.placeCamera();
    }

    buildField() {
      const R = this.root;

      const grassTex = fieldTexture();
      const grass = new THREE.Mesh(new THREE.PlaneGeometry(320, 320),
        new THREE.MeshStandardMaterial({ map: grassTex, roughness: 0.95 }));
      grass.rotation.x = -Math.PI / 2;
      grass.position.set(0, 0, 55);
      grass.receiveShadow = true;
      R.add(grass);

      const dirtTex = dirtTexture();
      const warning = new THREE.Mesh(new THREE.RingGeometry(94, 99.2, 48),
        new THREE.MeshStandardMaterial({ map: dirtTex, roughness: 0.95 }));
      warning.rotation.x = -Math.PI / 2;
      warning.position.y = 0.008;
      R.add(warning);

      const infield = new THREE.Mesh(new THREE.PlaneGeometry(42, 42),
        new THREE.MeshStandardMaterial({ map: dirtTex, roughness: 0.95 }));
      infield.rotation.x = -Math.PI / 2;
      infield.rotation.z = Math.PI / 4;
      infield.position.set(0, 0.01, 13.7);
      infield.receiveShadow = true;
      R.add(infield);

      const grassInfield = new THREE.Mesh(new THREE.PlaneGeometry(24, 24), mat(FIELD_GREEN_DARK));
      grassInfield.rotation.x = -Math.PI / 2;
      grassInfield.position.set(0, 0.015, 15.5);
      R.add(grassInfield);

      const mound = cyl(2.7, 2.9, 0.32, 0xa87f52);
      mound.position.set(0, 0.16, 18.44 - 0.6);
      mound.castShadow = true;
      R.add(mound);
      const rubber = box(0.7, 0.04, 0.15, LINE_WHITE);
      rubber.position.set(0, 0.34, 18.44 - 0.6);
      R.add(rubber);

      const homeCircle = cyl(1.3, 1.3, 0.02, 0xa87f52, 24);
      homeCircle.position.set(0, 0.012, 0);
      R.add(homeCircle);

      [[19.4, 19.4], [0, 38.8], [-19.4, 19.4]].forEach(([x, z]) => {
        const b = box(0.45, 0.1, 0.45, LINE_WHITE);
        b.position.set(x, 0.05, z);
        b.rotation.y = Math.PI / 4;
        R.add(b);
      });
      const platePts = [[0, -0.35], [-0.2, -0.1], [-0.2, 0.15], [0.2, 0.15], [0.2, -0.1]];
      const plateShape = new THREE.Shape(platePts.map(([x, z]) => new THREE.Vector2(x, z)));
      const plate = new THREE.Mesh(new THREE.ShapeGeometry(plateShape), mat(LINE_WHITE));
      plate.rotation.x = -Math.PI / 2;
      plate.rotation.z = Math.PI;
      plate.position.y = 0.03;
      R.add(plate);

      const batterBoxTex = null;
      [[-1, 1], [1, -1]].forEach(([side]) => {
        const bb = new THREE.Mesh(new THREE.RingGeometry(0.55, 0.62, 4, 1, 0, Math.PI * 2), mat(LINE_WHITE, { side: THREE.DoubleSide }));
        bb.rotation.x = -Math.PI / 2;
        bb.rotation.z = Math.PI / 4;
        bb.position.set(side * 0.95, 0.02, 0);
        R.add(bb);
      });

      [[-11.3, 1], [11.3, -1]].forEach(([dist, side]) => {
        const c = cyl(0.75, 0.75, 0.02, 0xa87f52, 20);
        c.position.set(side * 1.2 + (side > 0 ? 10 : -10), 0.014, 8);
        R.add(c);
      });

      [[1.5, 1.5, 72, 72], [-1.5, 1.5, -72, 72]].forEach(([x0, z0, x1, z1]) => {
        const len = Math.hypot(x1 - x0, z1 - z0);
        const line = box(0.32, 0.03, len, LINE_WHITE);
        line.position.set((x0 + x1) / 2, 0.025, (z0 + z1) / 2);
        line.rotation.y = Math.atan2(x1 - x0, z1 - z0);
        R.add(line);
      });

      this.buildBullpens();
    }

    buildBullpens() {
      [[-30, -1], [30, -1]].forEach(([x, side]) => {
        const pen = new THREE.Mesh(new THREE.PlaneGeometry(8, 14), mat(DIRT_DARK));
        pen.rotation.x = -Math.PI / 2;
        pen.position.set(x, 0.008, 62);
        R_add(this.root, pen);
        const fence = box(8, 1.1, 0.1, 0x3a4a5a);
        fence.position.set(x, 0.55, 69);
        R_add(this.root, fence);
      });
    }

    buildWall() {
      const R = this.root;
      const SEGS = 30;
      const wallMat = mat(0x1c2b45, { roughness: 0.7 });
      const distAt = (aDeg) => {
        const t = Math.abs(aDeg) / 45;
        return THREE.MathUtils.lerp(122, 99, Math.min(1, Math.max(0, (t - 0.55) / 0.45)));
      };
      for (let i = 0; i < SEGS; i++) {
        const a0 = THREE.MathUtils.lerp(-52, 52, i / SEGS) * Math.PI / 180;
        const a1 = THREE.MathUtils.lerp(-52, 52, (i + 1) / SEGS) * Math.PI / 180;
        const r0 = distAt(a0 * 180 / Math.PI), r1 = distAt(a1 * 180 / Math.PI);
        const p0 = new THREE.Vector3(Math.sin(a0) * r0, 0, Math.cos(a0) * r0);
        const p1 = new THREE.Vector3(Math.sin(a1) * r1, 0, Math.cos(a1) * r1);
        const mid = p0.clone().add(p1).multiplyScalar(0.5);
        const seg = box(p0.distanceTo(p1) + 0.5, 4.2, 1.0, 0x1c2b45);
        seg.position.set(mid.x, 2.1, mid.z);
        seg.rotation.y = Math.atan2(p1.x - p0.x, p1.z - p0.z);
        R.add(seg);
      }

      const padMat = mat(0x27406b);
      for (let i = 0; i < SEGS; i++) {
        const a0 = THREE.MathUtils.lerp(-52, 52, i / SEGS) * Math.PI / 180;
        const a1 = THREE.MathUtils.lerp(-52, 52, (i + 1) / SEGS) * Math.PI / 180;
        const r0 = distAt(a0 * 180 / Math.PI) - 0.55, r1 = distAt(a1 * 180 / Math.PI) - 0.55;
        const p0 = new THREE.Vector3(Math.sin(a0) * r0, 0, Math.cos(a0) * r0);
        const p1 = new THREE.Vector3(Math.sin(a1) * r1, 0, Math.cos(a1) * r1);
        const mid = p0.clone().add(p1).multiplyScalar(0.5);
        const pad = box(p0.distanceTo(p1) + 0.3, 2.4, 0.15, 0x27406b);
        pad.position.set(mid.x, 1.2, mid.z);
        pad.rotation.y = Math.atan2(p1.x - p0.x, p1.z - p0.z);
        R.add(pad);
      }

      const topLine = new THREE.Mesh(
        new THREE.TorusGeometry(110, 0.1, 6, 60, THREE.MathUtils.degToRad(104)),
        mat(0xf7d247, { emissive: 0x554400 }));
      topLine.position.set(0, 4.25, 0);
      topLine.scale.set(1, 1, 1.1);
      topLine.rotation.y = Math.PI / 2 + THREE.MathUtils.degToRad(-38);
      R.add(topLine);

      [[-45], [45]].forEach(([a]) => {
        const rad = a * Math.PI / 180;
        const pole = cyl(0.14, 0.18, 16, 0xf7d247);
        pole.position.set(Math.sin(rad) * 99.2, 8, Math.cos(rad) * 99.2);
        pole.castShadow = true;
        R.add(pole);
      });

      const eye = box(10, 6, 0.6, 0x0d1a0d);
      eye.position.set(0, 3, distAt(0) - 0.8);
      R.add(eye);
      const eyeLabel = new THREE.Mesh(
        new THREE.PlaneGeometry(8, 1.4),
        new THREE.MeshBasicMaterial({ map: makeCanvasTexture(512, 96, (ctx) => {
          ctx.fillStyle = '#0d1a0d'; ctx.fillRect(0, 0, 512, 96);
          ctx.fillStyle = '#c8ffc8'; ctx.font = 'bold 44px sans-serif';
          ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
          ctx.fillText('DIAMOND TILT', 256, 50);
        })}));
      eyeLabel.position.set(0, 4.6, distAt(0) - 1.15);
      R.add(eyeLabel);
    }

    buildStands() {
      const R = this.root;
      const seatColors = [0xd8574e, 0x4a6fd4, 0xe0c060, 0x4fae6a, 0xd8d8d8, 0xb06ac9];
      const concrete = mat(0x9aa2ad, { roughness: 0.9 });

      const tiers = [
        { rIn: 104, rOut: 128, y0: 1.8, y1: 8.5, count: 26 },
        { rIn: 130, rOut: 152, y0: 10.5, y1: 19, count: 22, infieldOnly: true },
      ];

      tiers.forEach((tier, ti) => {
        const rows = 8;
        for (let row = 0; row < rows; row++) {
          const t = row / (rows - 1);
          const r = THREE.MathUtils.lerp(tier.rIn, tier.rOut, t);
          const y = THREE.MathUtils.lerp(tier.y0, tier.y1, t);
          const arcSpan = tier.infieldOnly ? 150 : 250;
          const aStart = tier.infieldOnly ? -75 : -125;
          const ring = new THREE.Mesh(
            new THREE.CylinderGeometry(r, r, 0.55, 48, 1, true, (90 - aStart - arcSpan / 2) * Math.PI / 180, arcSpan * Math.PI / 180),
            concrete);
          ring.position.y = y;
          ring.rotation.y = Math.PI / 2;
          R.add(ring);
        }

        const seats = tier.count;
        const crowdGeo = new THREE.BoxGeometry(0.6, 1.0, 0.55);
        const crowdMat = new THREE.MeshStandardMaterial({ roughness: 0.9 });
        const perRow = Math.floor(seats / rows);
        const total = perRow * rows;
        const crowd = new THREE.InstancedMesh(crowdGeo, crowdMat, total);
        const dummy = new THREE.Object3D();
        const color = new THREE.Color();
        let idx = 0;
        for (let row = 0; row < rows; row++) {
          const t = row / (rows - 1);
          const r = THREE.MathUtils.lerp(tier.rIn, tier.rOut, t) - 0.4;
          const y = THREE.MathUtils.lerp(tier.y0, tier.y1, t) + 0.75;
          for (let i = 0; i < perRow && idx < total; i++, idx++) {
            const a = THREE.MathUtils.lerp(tier.infieldOnly ? -72 : -122, tier.infieldOnly ? 72 : 122, i / (perRow - 1)) * Math.PI / 180;
            dummy.position.set(Math.sin(a) * r, y, Math.cos(a) * r);
            dummy.rotation.y = a + Math.PI;
            dummy.updateMatrix();
            crowd.setMatrixAt(idx, dummy.matrix);
            crowd.setColorAt(idx, color.setHex(seatColors[(idx * 5 + row) % seatColors.length]).multiplyScalar(0.55 + Math.random() * 0.45));
          }
        }
        R.add(crowd);

        const facade = new THREE.Mesh(
          new THREE.CylinderGeometry(tier.rIn - 0.6, tier.rIn - 0.6, tier.y0 + 0.6, 48, 1, true, Math.PI / 4, Math.PI * 1.4),
          mat(0x7d8794, { roughness: 0.85, side: THREE.DoubleSide }));
        facade.position.y = (tier.y0) / 2;
        R.add(facade);
      });

      const roof = new THREE.Mesh(
        new THREE.CylinderGeometry(154, 130, 0.8, 48, 1, false, Math.PI / 4, Math.PI * 1.4),
        mat(0x39424e, { roughness: 0.6, metalness: 0.3 }));
      roof.position.y = 20.5;
      roof.rotation.x = Math.PI;
      R.add(roof);
    }

    buildLightTowers() {
      const R = this.root;
      [[-70, -30], [-95, 40], [70, -30], [95, 40], [-118, 100], [118, 100]].forEach(([aDeg, dist]) => {
        const rad = aDeg * Math.PI / 180;
        const x = Math.sin(rad) * dist;
        const z = Math.cos(rad) * dist;
        const pole = cyl(0.5, 0.7, 34, 0x4a525c);
        pole.position.set(x, 17, z);
        R.add(pole);
        const rig = box(9, 3.2, 0.7, 0x39424e);
        rig.position.set(x, 34, z);
        rig.lookAt(0, 0, 30);
        R.add(rig);
        for (let r = 0; r < 3; r++) {
          for (let c = 0; c < 6; c++) {
            const lamp = box(1.1, 0.8, 0.25, 0xfffbe0);
            lamp.material = new THREE.MeshStandardMaterial({
              color: 0xfffbe0, emissive: 0xfff2b0, emissiveIntensity: 0.9, roughness: 0.4,
            });
            lamp.position.set(x, 34 + (1 - r) * 1.15, z);
            const off = new THREE.Vector3((c - 2.5) * 1.35, 0, 0);
            off.applyQuaternion(rig.quaternion);
            lamp.position.add(off);
            lamp.lookAt(0, 0, 25);
            R.add(lamp);
          }
        }
        const glow = new THREE.PointLight(0xfff2cc, 0.5, 220, 1.6);
        glow.position.set(x, 33, z);
        R.add(glow);
      });
    }

    buildScoreboard() {
      const R = this.root;
      const canvas = document.createElement('canvas');
      canvas.width = 512; canvas.height = 256;
      const ctx = canvas.getContext('2d');
      this.scoreboardCanvas = canvas;

      const draw = () => {
        ctx.fillStyle = '#0a0e18';
        ctx.fillRect(0, 0, 512, 256);
        ctx.strokeStyle = '#2a3a5a'; ctx.lineWidth = 6;
        ctx.strokeRect(4, 4, 504, 248);
        ctx.fillStyle = '#ffd76a';
        ctx.font = 'bold 44px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('DIAMOND TILT', 256, 52);
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 58px monospace';
        ctx.fillText(this.scoreText || '0 : 0', 256, 130);
        ctx.font = 'bold 34px sans-serif';
        ctx.fillStyle = '#9fd0ff';
        ctx.fillText(this.scoreSub || '', 256, 190);
        ctx.fillStyle = '#5a6a8a';
        ctx.font = '24px sans-serif';
        ctx.fillText('HOME OF THE FANS', 256, 236);
      };
      draw();

      const tex = new THREE.CanvasTexture(canvas);
      this.scoreboardTexture = tex;

      const frame = box(26, 13.6, 0.8, 0x111827);
      const dist = 122;
      frame.position.set(0, 11.5, dist - 2);
      R.add(frame);
      const board = new THREE.Mesh(new THREE.PlaneGeometry(24, 12),
        new THREE.MeshBasicMaterial({ map: tex }));
      board.position.set(0, 11.5, dist - 1.55);
      board.rotation.y = Math.PI;
      R.add(board);
      const boardBack = new THREE.Mesh(new THREE.PlaneGeometry(24, 12), mat(0x1a2233));
      boardBack.position.set(0, 11.5, dist - 1.55 + 0.01);
      boardBack.rotation.y = 0;
      R.add(boardBack);
    }

    updateScoreboard(scoreText, subText) {
      this.scoreText = scoreText;
      this.scoreSub = subText;
      const ctx = this.scoreboardCanvas.getContext('2d');
      ctx.fillStyle = '#0a0e18'; ctx.fillRect(0, 0, 512, 256);
      ctx.strokeStyle = '#2a3a5a'; ctx.lineWidth = 6; ctx.strokeRect(4, 4, 504, 248);
      ctx.fillStyle = '#ffd76a'; ctx.font = 'bold 44px sans-serif'; ctx.textAlign = 'center';
      ctx.fillText('DIAMOND TILT', 256, 52);
      ctx.fillStyle = '#ffffff'; ctx.font = 'bold 58px monospace';
      ctx.fillText(this.scoreText || '0 : 0', 256, 130);
      ctx.font = 'bold 34px sans-serif'; ctx.fillStyle = '#9fd0ff';
      ctx.fillText(this.scoreSub || '', 256, 190);
      ctx.fillStyle = '#5a6a8a'; ctx.font = '24px sans-serif';
      ctx.fillText('HOME OF THE FANS', 256, 236);
      this.scoreboardTexture.needsUpdate = true;
    }

    buildDugouts() {
      const R = this.root;
      [[-1], [1]].forEach(([side]) => {
        const base = box(9, 0.6, 3.4, 0x2a3340);
        base.position.set(side * 7.5, 0.3, -2.2);
        R.add(base);
        const back = box(9, 2.2, 0.3, 0x1c2430);
        back.position.set(side * 7.5, 1.4, -3.6);
        R.add(back);
        const roof = box(9.6, 0.25, 3.8, 0x39424e);
        roof.position.set(side * 7.5, 2.6, -2.2);
        roof.castShadow = true;
        R.add(roof);
        const bench = box(7, 0.3, 0.7, 0x6b4226);
        bench.position.set(side * 7.5, 0.75, -3.1);
        R.add(bench);
      });
    }

    buildBackstop() {
      const R = this.root;
      const net = new THREE.Mesh(
        new THREE.PlaneGeometry(24, 9),
        new THREE.MeshStandardMaterial({
          color: 0x8a97a8, transparent: true, opacity: 0.16, side: THREE.DoubleSide,
          roughness: 0.9,
        }));
      net.position.set(0, 4.5, -14);
      R.add(net);
      [[-12, -14], [12, -14]].forEach(([x, z]) => {
        const post = cyl(0.12, 0.14, 9.5, 0x4a525c);
        post.position.set(x, 4.75, z);
        R.add(post);
      });
      const top = box(24.4, 0.14, 0.14, 0x4a525c);
      top.position.set(0, 9.4, -14);
      R.add(top);
    }

    buildBall() {
      const ball = sph(0.16, 0xffffff, 12);
      ball.castShadow = true;
      this.root.add(ball);
      this.ball = ball;
      this.ball.visible = false;

      const trailGeo = new THREE.BufferGeometry();
      this.trailPts = [];
      this.trailMax = 70;
      this.trailLine = new THREE.Line(trailGeo, new THREE.LineBasicMaterial({
        color: 0xffffff, transparent: true, opacity: 0.6,
      }));
      this.root.add(this.trailLine);
    }

    buildPlayers() {
      const defense = { uniform: 0x2a3f7d, pants: 0xf2f2f2, cap: true, glove: 'field' };
      const attack = { uniform: 0xd63c34, pants: 0xf2f2f2, helmet: true, bat: true };

      const mk = (name, o, x, z, ry, num) => {
        const opts = Object.assign({}, o, { number: num });
        const f = makeHumanoid(opts);
        f.root.position.set(x, 0, z);
        if (ry != null) f.root.rotation.y = ry;
        this.root.add(f.root);
        this.players[name] = f;
        return f;
      };

      mk('pitcher', Object.assign({}, defense, { glove: null }), 0, 17.6, Math.PI, 18);
      this.players.pitcher.root.position.y = 0.32;
      mk('catcher', Object.assign({}, defense, { glove: 'catcher', mask: true, chestProtector: true, cap: false }), 0, -1.7, Math.PI, 22);
      this.players.catcher.root.scale.setScalar(0.97);

      mk('1b', defense, 24, 26, Math.atan2(-24, -26 - 14) + Math.PI, 14);
      mk('2b', defense, 8, 33, Math.PI, 2);
      mk('ss', defense, -8, 33, Math.PI, 7);
      mk('3b', defense, -24, 26, Math.atan2(24, -26 - 14) + Math.PI, 5);
      mk('lf', defense, -34, 62, Math.atan2(34, 62 - 14) + Math.PI, 30);
      mk('cf', defense, 0, 72, Math.PI, 17);
      mk('rf', defense, 34, 62, Math.atan2(-34, 62 - 14) + Math.PI, 44);

      this.players.batter = mk('batter', attack, 0.85, 0.3, Math.PI + 0.2, 52);
      mk('ondeck', Object.assign({}, attack, { bat: true, helmet: true }), 2.6, -8.5, Math.PI, 8);

      mk('umpire', { uniform: 0x14161c, pants: 0x3a4048, cap: true, mask: true }, -0.7, -2.0, Math.PI, null);
    }

    placeCamera() {
      const camGO = window.__gameCamera;
      if (!camGO) return;
      camGO.position.set(0, 4.6, -8.5);
      camGO.rotation.set(0.28, 0, 0);
    }

    setRunnerBase(index, baseVec) {
      if (!this.runners[index]) {
        this.runners[index] = makeHumanoid({
          uniform: 0xd63c34, pants: 0xf2f2f2, helmet: true,
        });
        this.root.add(this.runners[index].root);
      }
      const r = this.runners[index];
      r.root.visible = true;
      r.root.position.set(baseVec.x + 0.4, 0, baseVec.z - 0.4);
    }

    hideRunner(index) {
      if (this.runners[index]) this.runners[index].root.visible = false;
    }

    updateRunnersFromState(state, outcome, animate) {
      const now = [state.first, state.second, state.third];
      const pos = [new THREE.Vector3(19.4, 0, 19.4), new THREE.Vector3(0, 0, 38.8), new THREE.Vector3(-19.4, 0, 19.4)];
      for (let i = 0; i < 3; i++) {
        if (now[i]) this.setRunnerBase(i, pos[i]);
        else if (!animate) this.hideRunner(i);
        else if (!now[i] && this.runners[i] && this.runners[i].root.visible && outcome &&
                 (outcome === 'Homerun' || outcome === 'Grounder' || outcome === 'DeepFly')) {
          this.runners[i].root.visible = false;
        }
      }
    }

    startPitchAnimation(durationSec) {
      this.pitchT = 0;
      this.pitchDur = Math.max(0.25, durationSec);
      this.pitchFlying = true;
      this.ball.visible = true;
      this.ball.position.set(0, 1.85, 17.8);
      if (this.trailLine) this.trailPts.length = 0;
      this.pitcherAnim = { t: 0 };
    }

    startContactAnimation(flight, origin) {
      this.pitchFlying = false;
      const pts = window.BallFlight.sample(flight, 30);
      const ox = origin ? origin.x : 0.2, oy = origin ? origin.y : 1.0, oz = origin ? origin.z : 0.4;
      this.contactPts = pts.map(p => new THREE.Vector3(p.x + ox, Math.max(0.08, p.y + oy), p.z + oz));
      this.contactT = 0;
      this.contactDur = Math.max(0.9, window.BallFlight.flightTimeNoDrag(flight));
      this.contactFlying = true;
      this.ball.visible = true;
      this.ball.position.copy(this.contactPts[0]);
      if (this.trailLine) this.trailPts.length = 0;
      if (this.players.batter.swingAnim) this.players.batter.swingT = 0;
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
        this.players.pitcher.legL.rotation.x = Math.sin(k * Math.PI) * 0.8;
        if (k >= 1) { this.pitcherAnim = null; arm.rotation.x = 0; this.players.pitcher.body.rotation.y = 0; this.players.pitcher.legL.rotation.x = 0; }
      }

      if (this.pitchFlying) {
        this.pitchT += dt / this.pitchDur;
        const t = Math.min(1, this.pitchT);
        const from = new THREE.Vector3(0, 1.85, 17.8);
        const to = new THREE.Vector3(0.2, 1.0, 0.4);
        this.ball.position.lerpVectors(from, to, t);
        this.ball.position.y += Math.sin(t * Math.PI) * 0.35;
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
      }

      const t = performance.now() / 1000;
      const batter = this.players.batter;
      if (batter.swingAnim) batter.swingAnim(dt);
      batter.body.rotation.y = Math.sin(t * 1.6) * 0.07;
      batter.torso.rotation.x = Math.sin(t * 2.1) * 0.02;

      [this.players['1b'], this.players['2b'], this.players['ss'], this.players['3b'],
       this.players.lf, this.players.cf, this.players.rf].forEach(f => {
        if (f) f.body.position.y = Math.sin(t * 1.8 + f.root.position.x) * 0.012;
      });

      this.runners.forEach(r => {
        if (!r || !r.root.visible) return;
        const ph = t * 10;
        r.legL.rotation.x = Math.sin(ph) * 0.95;
        r.legR.rotation.x = -Math.sin(ph) * 0.95;
        r.armL.rotation.x = -Math.sin(ph) * 0.75;
        r.armR.rotation.x = Math.sin(ph) * 0.75;
        r.body.position.y = Math.abs(Math.sin(ph)) * 0.05;
      });
    }

    pushTrail() {
      this.trailPts.push(this.ball.position.clone());
      if (this.trailPts.length > this.trailMax) this.trailPts.shift();
    }
  }

  function R_add(root, mesh) { root.add(mesh); }

  window.DTScene = { Stadium, makeHumanoid, fieldTexture, dirtTexture };
})();
