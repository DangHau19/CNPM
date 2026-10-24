(() => {
  'use strict';

  const API = '/api/public';
  const COOLDOWN_MS = 45 * 1000;        // sau khi đọc xong, phải chờ 45 giây (kể cả đã ra rồi vào lại vòng tròn) mới tự phát lại địa điểm đó
  const EXIT_MARGIN_M = 25;             // phải đi ra khỏi bán kính + 25 m thì lần sau vào lại mới phát tiếp (chống nhấp nháy GPS)
  const MAX_ACCURACY_M = 200;           // sai số GPS lớn hơn mức này thì chưa dùng để tự kích hoạt
  const PING_MS = 30 * 1000;            // báo "đang online" cho máy chủ mỗi 30 giây

  // Chuỗi giao diện theo ngôn ngữ (ngôn ngữ chưa có sẽ dùng tiếng Anh)
  const UI = {
    vi: { sub: 'Thuyết minh tự động theo vị trí GPS', gpsOn: 'Bật thuyết minh tự động', gpsOff: 'Tắt thuyết minh tự động',
          demo: 'Chế độ demo: nhấp lên bản đồ để di chuyển', demoHint: 'Chế độ demo: nhấp lên bản đồ để giả lập vị trí của bạn.',
          listen: 'Nghe', stop: 'Dừng', close: 'Đóng', near: 'Bạn đang ở gần', nearest: 'Gần nhất',
          none: 'Chưa có địa điểm nào trong phạm vi', waiting: 'Đang chờ vị trí GPS...',
          gpsDenied: 'Không thể lấy vị trí. Hãy cho phép truy cập vị trí trong cài đặt trình duyệt hoặc dùng chế độ demo.',
          gpsUnsupported: 'Thiết bị hoặc trình duyệt không hỗ trợ định vị.',
          weak: 'Tín hiệu GPS đang yếu (sai số lớn), chờ định vị chính xác hơn...',
          fallback: '(chưa có bản dịch, đang dùng ngôn ngữ khác)', noVoice: 'Thiết bị chưa có giọng đọc cho ngôn ngữ này.',
          tapToListen: '🔊 Chạm để nghe thuyết minh', auto: 'Tự động (theo thiết bị)',
          wTitle: 'Chào mừng đến Sài Gòn!',
          wText: 'Hãy cho phép truy cập vị trí. Khi bạn đến gần một địa điểm, ứng dụng sẽ tự động đọc và hiển thị thuyết minh, bạn không cần thao tác thêm. Nên giữ trang này mở khi di chuyển.',
          start: 'Bắt đầu',
          privacy: 'Ứng dụng ghi nhận ẩn danh thiết bị, ngôn ngữ và vị trí gần đúng để thống kê lượt truy cập.',
          serverErr: 'Không kết nối được máy chủ.',
          panelHide: 'Thu gọn bảng để xem bản đồ', panelShow: 'Mở bảng địa điểm' },
    en: { sub: 'Automatic GPS audio guide', gpsOn: 'Start auto guide', gpsOff: 'Stop auto guide',
          demo: 'Demo mode: click the map to move', demoHint: 'Demo mode: click the map to simulate your position.',
          listen: 'Listen', stop: 'Stop', close: 'Close', near: 'You are near', nearest: 'Nearest',
          none: 'No place in range yet', waiting: 'Waiting for GPS...',
          gpsDenied: 'Cannot get your location. Allow location access in your browser settings or use demo mode.',
          gpsUnsupported: 'This device or browser does not support geolocation.',
          weak: 'GPS signal is weak (low accuracy), waiting for a better fix...',
          fallback: '(no translation yet, using another language)', noVoice: 'This device has no voice for this language.',
          tapToListen: '🔊 Tap to listen', auto: 'Automatic (device language)',
          wTitle: 'Welcome to Saigon!',
          wText: 'Please allow location access. When you get close to a landmark, the app reads the narration aloud and shows the text automatically, with no extra taps. Keep this page open while you walk.',
          start: 'Start',
          privacy: 'The app anonymously records your device, language and approximate location for visit statistics.',
          serverErr: 'Cannot reach the server.',
          panelHide: 'Collapse panel to see the map', panelShow: 'Open places panel' },
    zh: { sub: 'GPS自动语音导览', gpsOn: '开启自动导览', gpsOff: '关闭自动导览',
          demo: '演示模式：点击地图移动位置', demoHint: '演示模式：点击地图模拟您的位置。',
          listen: '收听', stop: '停止', close: '关闭', near: '您正在靠近', nearest: '最近',
          none: '附近暂无景点', waiting: '正在等待GPS定位...',
          gpsDenied: '无法获取位置。请在浏览器设置中允许定位，或使用演示模式。',
          gpsUnsupported: '此设备或浏览器不支持定位。',
          weak: 'GPS信号较弱（误差较大），正在等待更精确的定位...',
          fallback: '（暂无该语言译文，已使用其他语言）', noVoice: '此设备没有该语言的语音。',
          tapToListen: '🔊 点击收听讲解', auto: '自动（跟随设备）',
          wTitle: '欢迎来到西贡！',
          wText: '请允许访问位置信息。当您靠近景点时，应用会自动朗读并显示讲解，无需任何操作。移动时请保持本页面打开。',
          start: '开始',
          privacy: '本应用会匿名记录设备、语言和大致位置，仅用于访问统计。',
          serverErr: '无法连接服务器。',
          panelHide: '收起面板以查看地图', panelShow: '展开地点面板' },
    ja: { sub: 'GPS自動音声ガイド', gpsOn: '自動ガイドを開始', gpsOff: '自動ガイドを停止',
          demo: 'デモモード：地図をクリックして移動', demoHint: 'デモモード：地図をクリックして現在地を再現します。',
          listen: '再生', stop: '停止', close: '閉じる', near: '近くにあります', nearest: '最寄り',
          none: '近くに名所はありません', waiting: 'GPS位置情報を待っています...',
          gpsDenied: '位置情報を取得できません。ブラウザの設定で位置情報を許可するか、デモモードをご利用ください。',
          gpsUnsupported: 'この端末またはブラウザは位置情報に対応していません。',
          weak: 'GPS信号が弱く誤差が大きいため、より正確な測位を待っています...',
          fallback: '（翻訳がないため別の言語を使用中）', noVoice: 'この端末にはこの言語の音声がありません。',
          tapToListen: '🔊 タップして音声ガイドを聞く', auto: '自動（端末の設定）',
          wTitle: 'サイゴンへようこそ！',
          wText: '位置情報へのアクセスを許可してください。名所に近づくと、ガイドが自動で読み上げられ、文章も表示されます。操作は不要です。移動中はこのページを開いたままにしてください。',
          start: '開始',
          privacy: '端末・言語・おおよその位置を匿名で記録し、アクセス統計に利用します。',
          serverErr: 'サーバーに接続できません。',
          panelHide: 'パネルを閉じて地図を見る', panelShow: 'スポット一覧を開く' },
    ko: { sub: 'GPS 자동 오디오 가이드', gpsOn: '자동 가이드 시작', gpsOff: '자동 가이드 중지',
          demo: '데모 모드: 지도를 클릭해 이동', demoHint: '데모 모드: 지도를 클릭해 내 위치를 시뮬레이션합니다.',
          listen: '듣기', stop: '정지', close: '닫기', near: '근처에 있습니다', nearest: '가장 가까운 곳',
          none: '근처에 명소가 없습니다', waiting: 'GPS 위치를 기다리는 중...',
          gpsDenied: '위치를 가져올 수 없습니다. 브라우저 설정에서 위치 권한을 허용하거나 데모 모드를 사용하세요.',
          gpsUnsupported: '이 기기 또는 브라우저는 위치 기능을 지원하지 않습니다.',
          weak: 'GPS 신호가 약해 오차가 큽니다. 더 정확한 위치를 기다리는 중...',
          fallback: '(번역이 없어 다른 언어를 사용 중)', noVoice: '이 기기에는 해당 언어의 음성이 없습니다.',
          tapToListen: '🔊 눌러서 해설 듣기', auto: '자동(기기 설정)',
          wTitle: '사이공에 오신 것을 환영합니다!',
          wText: '위치 접근을 허용해 주세요. 명소에 가까워지면 해설이 자동으로 재생되고 글로도 표시되므로 별도 조작이 필요 없습니다. 이동 중에는 이 페이지를 열어 두세요.',
          start: '시작',
          privacy: '기기, 언어, 대략적인 위치를 익명으로 기록하여 방문 통계에 사용합니다.',
          serverErr: '서버에 연결할 수 없습니다.',
          panelHide: '패널을 접고 지도 보기', panelShow: '장소 패널 열기' }
  };

  // ---------- Lưu trữ cục bộ (có thể bị chặn ở chế độ riêng tư → bọc try/catch) ----------
  const store = {
    get: k => { try { return localStorage.getItem(k); } catch { return null; } },
    set: (k, v) => { try { localStorage.setItem(k, v); } catch { /* bỏ qua */ } }
  };
  const session = {
    get: k => { try { return sessionStorage.getItem(k); } catch { return null; } },
    set: (k, v) => { try { sessionStorage.setItem(k, v); } catch { /* bỏ qua */ } }
  };

  function uuid() {
    if (window.crypto && crypto.randomUUID) return crypto.randomUUID();
    const b = new Uint8Array(16);
    if (window.crypto && crypto.getRandomValues) crypto.getRandomValues(b);
    else for (let i = 0; i < 16; i++) b[i] = Math.floor(Math.random() * 256);
    b[6] = (b[6] & 0x0f) | 0x40; b[8] = (b[8] & 0x3f) | 0x80;
    const h = [...b].map(x => x.toString(16).padStart(2, '0')).join('');
    return `${h.slice(0, 8)}-${h.slice(8, 12)}-${h.slice(12, 16)}-${h.slice(16, 20)}-${h.slice(20)}`;
  }

  function getId(storage, key) {
    let v = storage.get(key);
    if (!v) { v = uuid(); storage.set(key, v); }
    return v;
  }

  const state = {
    langPref: store.get('langPref') || store.get('lang') || 'auto',   // 'auto' = theo ngôn ngữ thiết bị
    lang: 'vi',
    languages: [],
    places: [],
    pos: null,              // { lat, lng, accuracy }
    realGps: false,         // vị trí hiện tại đến từ GPS thật (không phải chế độ demo)
    guide: false,           // đang bật thuyết minh tự động
    demo: false,
    watchId: null,
    playing: false,
    playToken: 0,
    audio: null,
    audioUnlocked: false,
    pending: null,          // đang chờ người dùng chạm lần đầu để trình duyệt cho phép phát âm thanh
    queue: [],              // các địa điểm vừa đi vào vùng nhưng phải chờ bài đang đọc xong
    inside: new Set(),      // các địa điểm đang nằm trong bán kính (để chỉ kích hoạt khi "đi vào")
    played: new Map(),      // placeId -> thời điểm đọc XONG gần nhất (hoặc lúc bắt đầu nếu đang đọc)
    current: null,          // địa điểm đang đọc (hoặc đang chờ chạm để đọc)
    sentences: [],
    wakeLock: null,
    panelOpen: true,        // bảng bên trái đang mở hay thu gọn
    clientId: getId(store, 'clientId'),          // định danh ẩn danh của thiết bị
    sessionKey: getId(session, 'sessionKey')     // định danh phiên (mỗi tab)
  };

  const $ = id => document.getElementById(id);
  const t = key => (UI[state.lang] || UI.en)[key] || UI.en[key] || key;

  let map, placeLayer, userMarker, firstFixCentered = false;

  // ---------- Tiện ích ----------
  async function api(path, options) {
    const res = await fetch(API + path, options);
    if (!res.ok) throw new Error('HTTP ' + res.status);
    return res.status === 204 ? null : res.json();
  }

  function distanceMeters(lat1, lon1, lat2, lon2) {
    const R = 6371000, rad = d => d * Math.PI / 180;
    const dLat = rad(lat2 - lat1), dLon = rad(lon2 - lon1);
    const a = Math.sin(dLat / 2) ** 2 + Math.cos(rad(lat1)) * Math.cos(rad(lat2)) * Math.sin(dLon / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }

  const setStatus = text => { $('status').textContent = text || ''; };

  // ---------- Ngôn ngữ: mặc định theo thiết bị, có thể đổi thủ công ----------
  function deviceLanguageCode() {
    const prefs = navigator.languages && navigator.languages.length ? navigator.languages : [navigator.language || 'en'];
    const active = new Set(state.languages.map(l => l.code.toLowerCase()));
    for (const tag of prefs) {
      const tl = String(tag).toLowerCase();
      if (active.has(tl)) return tl;                    // khớp đầy đủ
      const base = tl.split('-')[0];
      if (active.has(base)) return base;                // vi-VN → vi, zh-TW → zh
    }
    if (active.has('en')) return 'en';
    return state.languages.length ? state.languages[0].code : 'vi';
  }

  function resolveLang() {
    if (state.langPref !== 'auto' && state.languages.some(l => l.code === state.langPref)) return state.langPref;
    return deviceLanguageCode();
  }

  function renderLangSelect() {
    const sel = $('selLang');
    sel.innerHTML = '';
    const auto = document.createElement('option');
    auto.value = 'auto';
    const dev = state.languages.find(l => l.code === deviceLanguageCode());
    auto.textContent = t('auto') + (dev ? ' · ' + dev.nativeName : '');
    sel.appendChild(auto);
    state.languages.forEach(l => {
      const o = document.createElement('option');
      o.value = l.code;
      o.textContent = l.nativeName;
      sel.appendChild(o);
    });
    sel.value = state.languages.some(l => l.code === state.langPref) ? state.langPref : 'auto';
  }

  async function applyLanguage() {
    state.lang = resolveLang();
    closePlayer();
    renderLangSelect();
    applyUiText();
    try {
      await loadPlaces();
    } catch {
      setStatus(t('serverErr'));
    }
    ping();
  }

  // ---------- Giao diện ----------
  function applyUiText() {
    document.documentElement.lang = state.lang || 'vi';
    $('tSub').textContent = t('sub');
    $('tDemo').textContent = t('demo');
    $('btnGps').textContent = state.guide ? t('gpsOff') : t('gpsOn');
    $('btnTap').textContent = t('tapToListen');
    $('btnStop').textContent = state.playing || state.pending ? t('stop') : t('close');
    $('wTitle').textContent = t('wTitle');
    $('wText').textContent = t('wText');
    $('btnStart').textContent = t('start');
    $('privacy').textContent = t('privacy');
    $('wPrivacy').textContent = t('privacy');
    updatePanelButton();
  }

  // ---------- Bảng bên trái (thu gọn / mở rộng) ----------
  function updatePanelButton() {
    const btn = $('btnPanel');
    const label = t(state.panelOpen ? 'panelHide' : 'panelShow');
    btn.textContent = state.panelOpen ? '◀' : '▶';
    btn.title = label;
    btn.setAttribute('aria-label', label);
    btn.setAttribute('aria-expanded', String(state.panelOpen));
  }

  /** remember = true khi do người dùng bấm nút (lưu lựa chọn cho lần mở trang sau). */
  function setPanel(open, remember) {
    state.panelOpen = open;
    $('panel').classList.toggle('collapsed', !open);
    $('panelBody').inert = !open;    // phần nội dung đang ẩn thì không nhận phím Tab
    updatePanelButton();
    if (remember) store.set('panelOpen', open ? '1' : '0');
  }

  // ---------- Bản đồ ----------
  function initMap() {
    map = L.map('map', { zoomControl: false }).setView([10.7769, 106.7009], 15);
    L.control.zoom({ position: 'topright' }).addTo(map);   // bảng nổi nằm bên trái nên đặt nút zoom bên phải
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);
    placeLayer = L.layerGroup().addTo(map);

    map.on('click', e => {
      if (!state.demo) return;
      if (!state.guide) startGuide();
      setPosition(e.latlng.lat, e.latlng.lng, 5, false);
    });
  }

  function renderMarkers() {
    placeLayer.clearLayers();
    state.places.forEach(p => {
      L.circle([p.latitude, p.longitude], { radius: p.radiusMeters, color: '#c2410c', weight: 1, fillOpacity: 0.08 }).addTo(placeLayer);
      L.marker([p.latitude, p.longitude]).addTo(placeLayer)
        .bindTooltip(p.name)
        .on('click', () => manualPlay(p));
    });
  }

  function renderList() {
    const list = $('list');
    list.innerHTML = '';
    const items = [...state.places];
    if (state.pos) items.sort((a, b) => a.distanceMeters - b.distanceMeters);
    items.forEach(p => {
      const li = document.createElement('li');
      if (state.pos && p.distanceMeters <= p.radiusMeters) li.className = 'inrange';

      const meta = document.createElement('div');
      meta.className = 'meta';
      const name = document.createElement('strong');
      name.textContent = p.name;
      const addr = document.createElement('small');
      addr.textContent = p.address + (state.pos ? ' · ' + Math.round(p.distanceMeters) + ' m' : '');
      meta.append(name, addr);

      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'primary';
      btn.textContent = '▶ ' + t('listen');
      btn.onclick = () => manualPlay(p);

      li.append(meta, btn);
      list.appendChild(li);
    });
  }

  async function loadPlaces() {
    state.places = await api('/places?' + new URLSearchParams({ lang: state.lang }));
    refreshDistances();
    renderMarkers();
    renderList();
    if (state.pos) evaluate();
  }

  function refreshDistances() {
    state.places.forEach(p => {
      p.distanceMeters = state.pos ? distanceMeters(state.pos.lat, state.pos.lng, p.latitude, p.longitude) : Infinity;
    });
  }

  // ---------- GPS ----------
  function setPosition(lat, lng, accuracy, real) {
    state.pos = { lat, lng, accuracy };
    state.realGps = real;
    if (!userMarker) {
      userMarker = L.circleMarker([lat, lng], { radius: 9, color: '#fff', weight: 3, fillColor: '#2563eb', fillOpacity: 1 }).addTo(map);
    } else {
      userMarker.setLatLng([lat, lng]);
    }
    if (real && !firstFixCentered) { firstFixCentered = true; map.setView([lat, lng], 16); }
    refreshDistances();
    renderList();
    evaluate();
  }

  function onFix(p) {
    if (state.demo) return;
    setPosition(p.coords.latitude, p.coords.longitude, p.coords.accuracy || 0, true);
  }

  function onGeoError(err) {
    if (err && err.code === 1) setStatus(t('gpsDenied'));   // PERMISSION_DENIED
    // Hết thời gian / tạm thời mất tín hiệu: watchPosition tự thử lại, không cần báo lỗi.
  }

  function applyGpsMode() {
    if (state.watchId != null) {
      navigator.geolocation.clearWatch(state.watchId);
      state.watchId = null;
    }
    if (!state.guide) return;
    if (state.demo) { setStatus(t('demoHint')); return; }
    if (!navigator.geolocation) { setStatus(t('gpsUnsupported')); return; }
    setStatus(t('waiting'));
    state.watchId = navigator.geolocation.watchPosition(onFix, onGeoError,
      { enableHighAccuracy: true, maximumAge: 5000, timeout: 20000 });
  }

  /**
   * Kiểm tra vị trí hiện tại. Chỉ kích hoạt khi người dùng "đi vào" bán kính của một địa điểm
   * (không phát lặp lại khi đứng yên) và đã quá thời gian chờ (cooldown) kể từ lần phát trước.
   */
  function evaluate() {
    if (!state.pos || !state.guide) return;
    if (state.realGps && state.pos.accuracy > MAX_ACCURACY_M) { setStatus(t('weak')); return; }   // chưa đủ chính xác: chưa ghi nhận "đi vào"

    const entering = [];
    state.places.forEach(p => {
      if (p.distanceMeters <= p.radiusMeters) {
        if (!state.inside.has(p.id)) { state.inside.add(p.id); entering.push(p); }
      } else if (p.distanceMeters > p.radiusMeters + (state.demo ? 0 : EXIT_MARGIN_M)) {   // demo: ra khỏi vòng tròn là tính "đã rời"
        state.inside.delete(p.id);
      }
    });

    const sorted = [...state.places].sort((a, b) => a.distanceMeters - b.distanceMeters);
    const hit = sorted.find(p => p.distanceMeters <= p.radiusMeters);
    if (hit) setStatus(t('near') + ': ' + hit.name);
    else if (sorted.length) setStatus(t('none') + ' · ' + t('nearest') + ': ' + sorted[0].name + ' (' + Math.round(sorted[0].distanceMeters) + ' m)');
    else setStatus(t('none'));

    entering.sort((a, b) => a.distanceMeters - b.distanceMeters).forEach(p => {
      if (!state.demo) {   // demo: phát lại lập tức, không áp dụng thời gian chờ
        const last = state.played.get(p.id) || 0;
        const busyHere = (state.playing || state.pending) && state.current && state.current.id === p.id;
        if (busyHere || Date.now() - last < COOLDOWN_MS) return;   // đang đọc chính nơi này, hoặc chưa đủ 45 giây sau khi đọc xong
      }
      if (state.playing || state.pending) {
        if (!state.queue.some(q => q.place.id === p.id)) state.queue.push({ place: p, trigger: 'gps' });
      } else {
        play(p, 'gps');
      }
    });
  }

  // ---------- Bật / tắt thuyết minh tự động ----------
  function startGuide() {
    state.guide = true;
    store.set('guideStarted', '1');
    $('overlay').hidden = true;
    state.inside.clear();       // đang đứng sẵn trong một địa điểm khi bật → được tính là "đi vào"
    applyUiText();
    applyGpsMode();
    requestWakeLock();
    if (state.pos) evaluate();
  }

  function stopGuide() {
    state.guide = false;
    store.set('guideStarted', '0');
    state.queue = [];
    applyGpsMode();
    setStatus('');
    closePlayer();
    applyUiText();
    releaseWakeLock();
  }

  async function requestWakeLock() {
    try {
      if ('wakeLock' in navigator && !state.wakeLock) {
        state.wakeLock = await navigator.wakeLock.request('screen');
        state.wakeLock.addEventListener('release', () => { state.wakeLock = null; });
      }
    } catch { /* không bắt buộc */ }
  }

  function releaseWakeLock() {
    try { if (state.wakeLock) state.wakeLock.release(); } catch { /* bỏ qua */ }
    state.wakeLock = null;
  }

  // ---------- Âm thanh ----------
  /** Trình duyệt chỉ cho phát tiếng sau khi người dùng đã chạm vào trang ít nhất một lần. */
  function canAutoplay() {
    return state.audioUnlocked || (navigator.userActivation && navigator.userActivation.hasBeenActive === true);
  }

  function unlockAudio() {
    state.audioUnlocked = true;
    try {
      if ('speechSynthesis' in window) {
        const u = new SpeechSynthesisUtterance(' ');
        u.volume = 0;
        window.speechSynthesis.speak(u);
      }
    } catch { /* bỏ qua */ }
  }

  function manualPlay(place) {
    unlockAudio();
    play(place, 'manual');
  }

  function play(place, trigger) {
    stopPlayback();
    const token = ++state.playToken;
    state.played.set(place.id, Date.now());
    state.current = place;
    showPlayer(place);

    if (!canAutoplay()) {
      // Chưa có thao tác chạm nào: hiện chữ ngay, âm thanh sẽ phát ở lần chạm đầu tiên.
      state.pending = { place, trigger };
      $('plNote').textContent = t('tapToListen');
      $('btnTap').hidden = false;
      $('btnStop').textContent = t('stop');
      return;
    }
    begin(place, trigger, token);
  }

  function begin(place, trigger, token) {
    state.playing = true;
    state.pending = null;
    $('btnTap').hidden = true;
    $('btnStop').textContent = t('stop');
    $('plNote').textContent = place.fellBack ? t('fallback') : '';
    logPlay(place, trigger);

    const done = () => {
      if (token !== state.playToken) return;   // lần phát này đã bị thay thế/dừng
      state.playing = false;
      state.current = null;
      state.played.set(place.id, Date.now());   // tính 45 giây chờ từ lúc đọc xong
      highlight(-1);
      $('btnStop').textContent = t('close');
      playNextInQueue();
    };

    if (place.audioUrl) {
      let fellBack = false;
      const fallbackToSpeech = () => {
        if (fellBack || token !== state.playToken) return;
        fellBack = true;
        speak(place, token, done);
      };
      state.audio = new Audio(place.audioUrl);
      state.audio.onended = done;
      state.audio.onerror = fallbackToSpeech;
      state.audio.play().catch(fallbackToSpeech);
    } else {
      speak(place, token, done);
    }
  }

  function playNextInQueue() {
    while (state.queue.length) {
      const next = state.queue.shift();
      if (state.inside.has(next.place.id)) { play(next.place, next.trigger); return; }   // bỏ qua nơi đã đi qua
    }
  }

  function pickVoice(locale) {
    const voices = window.speechSynthesis.getVoices();
    const norm = v => v.lang.replace('_', '-').toLowerCase();
    const l = locale.toLowerCase(), base = l.split('-')[0];
    return voices.find(v => norm(v) === l) || voices.find(v => norm(v).startsWith(base)) || null;
  }

  function speak(place, token, done) {
    if (!('speechSynthesis' in window)) {
      // Không có giọng đọc: vẫn hiển thị chữ, tự chuyển bài sau thời gian đọc ước lượng.
      setTimeout(done, Math.max(4000, place.narrationText.length * 70));
      return;
    }
    if (state.sentences.length === 0) { done(); return; }
    const synth = window.speechSynthesis;
    const voice = pickVoice(place.speechLocale);
    if (!voice && synth.getVoices().length > 0) $('plNote').textContent = t('noVoice');

    // Tách câu để tránh lỗi trình duyệt tự dừng khi đọc đoạn dài, đồng thời tô sáng câu đang đọc.
    state.sentences.forEach((sentence, i) => {
      const u = new SpeechSynthesisUtterance(sentence);
      u.lang = place.speechLocale;
      if (voice) u.voice = voice;
      u.rate = 0.95;
      u.onstart = () => { if (token === state.playToken) highlight(i); };
      if (i === state.sentences.length - 1) { u.onend = done; u.onerror = done; }
      synth.speak(u);
    });
  }

  function splitSentences(text) {
    return (text.match(/[^.!?。！？]+[.!?。！？]*/g) || [text]).map(s => s.trim()).filter(Boolean);
  }

  function showPlayer(place) {
    $('plName').textContent = place.name;
    $('plNote').textContent = '';
    const box = $('plText');
    box.textContent = '';
    state.sentences = splitSentences(place.narrationText);
    state.sentences.forEach(s => {
      const sp = document.createElement('span');
      sp.className = 's';
      sp.textContent = s + ' ';
      box.appendChild(sp);
    });
    $('btnTap').hidden = true;
    $('player').hidden = false;
    if (!state.panelOpen) setPanel(true, false);   // có thuyết minh mới: tự mở bảng để hiện chữ (không ghi đè lựa chọn đã lưu)
  }

  function highlight(index) {
    Array.from($('plText').children).forEach((el, i) => el.classList.toggle('cur', i === index));
  }

  function stopPlayback() {
    state.playToken++;   // vô hiệu hóa mọi callback của lần phát trước
    state.playing = false;
    state.current = null;
    state.pending = null;
    if (state.audio) { state.audio.pause(); state.audio = null; }
    if ('speechSynthesis' in window) window.speechSynthesis.cancel();
  }

  function closePlayer() {
    stopPlayback();
    state.queue = [];
    $('player').hidden = true;
  }

  async function logPlay(place, trigger) {
    try {
      await fetch(API + '/play-log', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ placeId: place.id, languageCode: place.languageCode, triggerType: trigger, clientId: state.clientId })
      });
    } catch { /* ghi log lỗi không ảnh hưởng trải nghiệm */ }
  }

  // ---------- Báo cho máy chủ biết có người đang truy cập ----------
  let pinging = false, pingAgain = false;
  function ping() {
    // Tránh gửi chồng nhiều gói cùng lúc (mở trang + đổi ngôn ngữ + quay lại tab); gói đến khi đang bận sẽ được gửi ngay sau đó.
    if (pinging) { pingAgain = true; return; }
    pinging = true;
    const withPos = state.realGps && state.pos;
    fetch(API + '/visits/ping', {
      method: 'POST',
      keepalive: true,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        clientId: state.clientId,
        sessionKey: state.sessionKey,
        deviceLanguage: navigator.language || '',
        languageCode: state.lang,
        gpsEnabled: !!withPos,
        latitude: withPos ? state.pos.lat : null,
        longitude: withPos ? state.pos.lng : null
      })
    }).catch(() => { /* không ảnh hưởng trải nghiệm */ })
      .finally(() => { pinging = false; if (pingAgain) { pingAgain = false; ping(); } });
  }

  // ---------- Khởi tạo ----------
  function bindEvents() {
    $('selLang').onchange = async e => {
      state.langPref = e.target.value;
      store.set('langPref', state.langPref);
      await applyLanguage();
    };

    $('btnPanel').onclick = () => setPanel(!state.panelOpen, true);

    $('btnStop').onclick = closePlayer;

    $('btnTap').onclick = () => {
      unlockAudio();
      const p = state.pending;
      if (p) play(p.place, p.trigger);
    };

    $('btnStart').onclick = () => { unlockAudio(); startGuide(); };

    $('btnGps').onclick = () => {
      if (state.guide) { stopGuide(); return; }
      unlockAudio();
      startGuide();
    };

    $('chkDemo').onchange = e => {
      state.demo = e.target.checked;
      state.pos = null;
      state.realGps = false;
      state.inside.clear();
      refreshDistances();
      renderList();
      if (state.demo && !state.guide) { unlockAudio(); startGuide(); }
      else applyGpsMode();
    };

    // Lần chạm đầu tiên vào trang: mở khóa âm thanh và phát bài đang chờ (nếu có).
    const onFirstGesture = () => {
      if (state.audioUnlocked) return;
      unlockAudio();
      if (state.pending) { const p = state.pending; play(p.place, p.trigger); }
    };
    ['pointerdown', 'touchstart', 'keydown'].forEach(ev =>
      window.addEventListener(ev, onFirstGesture, { once: true, passive: true }));

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState !== 'visible') return;
      if (state.guide) requestWakeLock();
      ping();
    });
    setInterval(() => { if (document.visibilityState === 'visible') ping(); }, PING_MS);

    // Nếu người dùng cấp quyền vị trí sau khi đã bị từ chối, tự bắt đầu theo dõi lại.
    try {
      if (navigator.permissions && navigator.permissions.query) {
        navigator.permissions.query({ name: 'geolocation' }).then(s => {
          s.onchange = () => { if (s.state === 'granted' && state.guide && !state.demo) applyGpsMode(); };
        }).catch(() => {});
      }
    } catch { /* bỏ qua */ }
  }

  async function init() {
    // Lần đầu: màn hình rộng thì mở bảng, điện thoại thì thu gọn để thấy bản đồ; sau đó nhớ theo lựa chọn của người dùng.
    const savedPanel = store.get('panelOpen');
    const openByDefault = savedPanel === null ? !window.matchMedia('(max-width: 700px)').matches : savedPanel === '1';
    initMap();
    bindEvents();
    setPanel(openByDefault, false);
    requestAnimationFrame(() => requestAnimationFrame(() => $('panel').classList.remove('no-anim')));   // từ giờ mới bật hiệu ứng trượt
    try {
      state.languages = await api('/languages');
    } catch {
      applyUiText();
      setStatus(t('serverErr'));
      return;
    }
    await applyLanguage();

    const started = store.get('guideStarted');
    if (started === '1') startGuide();                       // đã từng bắt đầu → tự theo dõi GPS ngay khi mở trang
    else if (started === null) $('overlay').hidden = false;  // lần đầu: màn hình chào + xin quyền
  }

  init();
})();
