// Game bootstrap: renderer, loop, input, HUD bindings
(function () {
  'use strict';

  const TPS = 60;

  const canvas = document.getElementById('game');
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.outputEncoding = THREE.sRGBEncoding;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.15;

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0b1220);
  scene.fog = new THREE.Fog(0x0b1220, 120, 320);

  const camera = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 0.1, 600);
  const CAM_HOME = new THREE.Vector3(0, 4.4, -8.6);
  camera.position.copy(CAM_HOME);
  camera.lookAt(new THREE.Vector3(0, 1.4, 14));

  window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
  });

  scene.add(new THREE.HemisphereLight(0x8fa8d8, 0x1d4a20, 0.55));
  const sun = new THREE.DirectionalLight(0xfff4e0, 1.35);
  sun.position.set(-30, 48, -20);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  sun.shadow.camera.left = -60; sun.shadow.camera.right = 60;
  sun.shadow.camera.top = 80; sun.shadow.camera.bottom = -30;
  scene.add(sun);
  const fill = new THREE.DirectionalLight(0xbcd0ff, 0.35);
  fill.position.set(30, 30, 40);
  scene.add(fill);

  const stadium = new window.DTScene.Stadium(scene);

  // ---- session ----
  let coins = parseInt(localStorage.getItem('dt.coins') || '0', 10) || 0;
  const addCoins = (n) => { coins += n; localStorage.setItem('dt.coins', String(coins)); };
  const session = new window.DTSession.MatchPlaySession(
    (Math.random() * 2147483647) | 0,
    (result) => {
      addCoins(result === 'Home' ? 100 : 30);
    });
  stadium.updateRunnersFromState(session.state, null, false);

  // ---- input ----
  let pendingSpeed = 1;
  const zoneGrid = document.getElementById('zoneGrid');
  const zoneButtons = [];
  for (let row = 0; row < 3; row++) {
    for (let col = 0; col < 3; col++) {
      const b = document.createElement('button');
      b.className = 'zone';
      b.textContent = row * 3 + col + 1;
      b.addEventListener('click', () => {
        if (session.playerPitch(row * 3 + col + 1, pendingSpeed)) hideZones();
      });
      zoneGrid.appendChild(b);
      zoneButtons.push(b);
    }
  }
  document.querySelectorAll('.speed').forEach(btn => {
    btn.addEventListener('click', () => {
      pendingSpeed = parseInt(btn.dataset.speed, 10);
      document.querySelectorAll('.speed').forEach(b => b.classList.remove('sel'));
      btn.classList.add('sel');
    });
  });
  function showZones() { zoneGrid.style.display = 'grid'; }
  function hideZones() { zoneGrid.style.display = 'none'; }

  const swingBtn = document.getElementById('swingBtn');
  const trySwing = () => { if (session.playerSwing()) { swingBtn.classList.add('hit'); setTimeout(() => swingBtn.classList.remove('hit'), 180); } };
  swingBtn.addEventListener('click', trySwing);
  canvas.addEventListener('pointerdown', (e) => {
    if (e.target === canvas) trySwing();
  });
  window.addEventListener('keydown', (e) => {
    if (e.code === 'Space') { e.preventDefault(); trySwing(); }
  });

  document.getElementById('againBtn').addEventListener('click', () => {
    location.reload();
  });

  // ---- HUD ----
  const el = (id) => document.getElementById(id);
  const logList = el('log');
  function log(text) {
    const li = document.createElement('li');
    li.textContent = text;
    logList.prepend(li);
    while (logList.children.length > 5) logList.removeChild(logList.lastChild);
  }
  function describe(e) {
    const half = e.isTop ? 'Top' : 'Bot';
    const side = e.isTop ? 'Away' : 'Home';
    switch (e.type) {
      case 'BallCalled': return `${half} ${e.inning}: Ball`;
      case 'Foul': return `${e.inning}${half} Foul`;
      case 'StrikeCalled': return `Strike`;
      case 'BatterWalked': return `Walk!`;
      case 'BatterStruckOut': return `Strikeout!`;
      case 'BatterOut': return `Batter out`;
      case 'RunnerOut': return `Runner out!`;
      case 'HitRecorded': return `${side}: Hit!`;
      case 'HomerunRecorded': return `${side}: HOME RUN!!!`;
      case 'RunScored': return `${side} scores!`;
      case 'HalfInningEnded': return `End ${e.inning}${half}`;
      case 'MatchEnded': return 'Final';
      default: return e.type;
    }
  }

  function updateHUD() {
    const s = session.state;
    el('inning').textContent = `${s.isTop ? 'Top' : 'Bot'} ${s.inning}`;
    el('count').textContent = `${s.balls}-${s.strikes}`;
    el('outs').textContent = `OUTS ${s.outs}`;
    el('score').textContent = `${s.awayRuns} : ${s.homeRuns}`;
    el('coins').textContent = `🪙 ${coins}`;
    ['first', 'second', 'third'].forEach(k => {
      el('base_' + k).classList.toggle('on', s[k]);
    });

    const over = session.phase === 'MatchOver';
    el('result').style.display = over ? 'flex' : 'none';
    if (over) {
      const r = s.result === 'Home' ? '🏆 VICTORY!' : s.result === 'Away' ? '💀 DEFEAT' : '🤝 DRAW';
      el('resultTitle').textContent = r;
      el('resultScore').textContent = `Final score ${s.awayRuns} : ${s.homeRuns}`;
    }

    const batting = session.playerBatting;
    el('battingLabel').textContent = batting ? 'BAT — watch the ball, then SWING!' : 'DEFENSE — pick a zone to pitch';
    swingBtn.style.display = batting && session.phase === 'BallIncoming' ? 'block' : 'none';
    if (session.phase === 'WaitingToPitch' && s.isTop) showZones(); else hideZones();
  }

  // ---- loop ----
  let lastPhase = session.phase;
  let acc = 0;
  let lastT = performance.now();
  let camTarget = new THREE.Vector3(0, 1.4, 14);
  let lastBoardSig = '';

  function frame(now) {
    requestAnimationFrame(frame);
    const dt = Math.min(0.1, (now - lastT) / 1000);
    lastT = now;

    if (session.phase !== 'MatchOver') {
      acc += dt * TPS;
      const whole = Math.floor(acc);
      if (whole > 0) {
        acc -= whole;
        session.tickAdvance(whole);
      }
    }

    const phaseChanged = lastPhase !== session.phase;
    if (phaseChanged) {
      if (session.phase === 'BallIncoming')
        stadium.startPitchAnimation(session.flightTicks / session.tps);
      if (session.phase === 'BetweenPlays') {
        if (session.lastContactWasSwing && session.lastContactFlight) {
          stadium.startContactAnimation(session.lastContactFlight);
          const outcome = session.lastOutcome;
          setTimeout(() => {
            stadium.updateRunnersFromState(session.state, outcome, true);
            if (outcome === 'Single' || outcome === 'LineSingle' || outcome === 'Double' ||
                outcome === 'Triple' || outcome === 'Homerun') {
              stadium.animateBatterRunner(outcome);
            }
          }, Math.max(700, window.BallFlight.flightTimeNoDrag(session.lastContactFlight) * 1000 * 0.7));
        } else {
          stadium.ballToCatcher();
          setTimeout(() => stadium.updateRunnersFromState(session.state, null, false), 350);
        }
      }
      lastPhase = session.phase;
    }

    for (const e of session.drainEvents()) {
      log(describe(e));
      if (e.type === 'HalfInningEnded') stadium.updateRunnersFromState(session.state, null, false);
    }

    {
      const s2 = session.state;
      const sig = `${s2.awayRuns}:${s2.homeRuns}|${s2.isTop?'T':'B'}${s2.inning}|${s2.balls}-${s2.strikes}-${s2.outs}`;
      if (sig !== lastBoardSig && typeof stadium.updateScoreboard === 'function') {
        lastBoardSig = sig;
        stadium.updateScoreboard(`${s2.awayRuns} : ${s2.homeRuns}`,
          `${s2.isTop ? 'Top' : 'Bot'} ${s2.inning} · ${s2.balls}-${s2.strikes} · OUTS ${s2.outs}`);
      }
    }

    stadium.update(dt, session);

    if (stadium.ball.visible && (stadium.contactFlying || stadium.pitchFlying)) {
      camTarget.lerp(stadium.ball.position, 0.06);
    } else {
      camTarget.lerp(new THREE.Vector3(0, 1.4, 14), 0.04);
    }
    camera.lookAt(camTarget);

    updateHUD();
    renderer.render(scene, camera);
  }
  requestAnimationFrame(frame);
})();
