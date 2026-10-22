(() => {
  'use strict';

  const API = '/api/public';
  const COOLDOWN_MS = 10 * 60 * 1000;   // không phát lại cùng một địa điểm trong 10 phút (chế độ tự động)

  // Chuỗi giao diện theo ngôn ngữ (ngôn ngữ chưa có sẽ dùng tiếng Anh)
  const UI = {
    vi: { sub: 'Thuyết minh tự động theo vị trí GPS', gpsOn: 'Bật thuyết minh tự động', gpsOff: 'Tắt thuyết minh tự động',
          demo: 'Chế độ demo: nhấp lên bản đồ để di chuyển', listen: 'Nghe', stop: 'Dừng', near: 'Bạn đang ở gần',
          none: 'Chưa có địa điểm nào trong phạm vi', waiting: 'Đang chờ vị trí GPS...',
          gpsDenied: 'Không thể lấy vị trí. Hãy cấp quyền định vị hoặc dùng chế độ demo.',
          fallback: '(chưa có bản dịch, đang dùng ngôn ngữ khác)', noVoice: 'Thiết bị chưa có giọng đọc cho ngôn ngữ này.', change: 'Đổi ngôn ngữ' },
    en: { sub: 'Automatic GPS audio guide', gpsOn: 'Start auto guide', gpsOff: 'Stop auto guide',
          demo: 'Demo mode: click the map to move', listen: 'Listen', stop: 'Stop', near: 'You are near',
          none: 'No place in range yet', waiting: 'Waiting for GPS...',
          gpsDenied: 'Cannot get your location. Allow location access or use demo mode.',
          fallback: '(no translation yet, using another language)', noVoice: 'This device has no voice for this language.', change: 'Language' },
    zh: { sub: 'GPS自动语音导览', gpsOn: '开启自动导览', gpsOff: '关闭自动导览',
          demo: '演示模式：点击地图移动位置', listen: '收听', stop: '停止', near: '您正在靠近',
          none: '附近暂无景点', waiting: '正在等待GPS定位...',
          gpsDenied: '无法获取位置，请允许定位或使用演示模式。',
          fallback: '（暂无该语言译文，已使用其他语言）', noVoice: '此设备没有该语言的语音。', change: '语言' },
    ja: { sub: 'GPS自動音声ガイド', gpsOn: '自動ガイドを開始', gpsOff: '自動ガイドを停止',
          demo: 'デモモード：地図をクリックして移動', listen: '再生', stop: '停止', near: '近くにあります',
          none: '近くに名所はありません', waiting: 'GPS位置情報を待っています...',
          gpsDenied: '位置情報を取得できません。許可するかデモモードをご利用ください。',
          fallback: '（翻訳がないため別の言語を使用中）', noVoice: 'この端末にはこの言語の音声がありません。', change: '言語' },
    ko: { sub: 'GPS 자동 오디오 가이드', gpsOn: '자동 가이드 시작', gpsOff: '자동 가이드 중지',
          demo: '데모 모드: 지도를 클릭해 이동', listen: '듣기', stop: '정지', near: '근처에 있습니다',
          none: '근처에 명소가 없습니다', waiting: 'GPS 위치를 기다리는 중...',
          gpsDenied: '위치를 가져올 수 없습니다. 위치 권한을 허용하거나 데모 모드를 사용하세요.',
          fallback: '(번역이 없어 다른 언어를 사용 중)', noVoice: '이 기기에는 해당 언어의 음성이 없습니다.', change: '언어' }
  };

  const state = {
    lang: localStorage.getItem('lang'),
    languages: [],
    places: [],
    pos: null,
    gpsOn: false,
    demo: false,
    watchId: null,
    playing: false,
    playToken: 0,
    audio: null,
    played: new Map()   // placeId -> timestamp lần phát gần nhất
  };

  const $ = id => document.getElementById(id);
  const t = key => (UI[state.lang] || UI.en)[key] || UI.en[key] || key;

  let map, placeLayer, userMarker;

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

  // ---------- Giao diện ----------
  function applyUiText() {
    document.documentElement.lang = state.lang || 'vi';
    $('tSub').textContent = t('sub');
    $('tDemo').textContent = t('demo');
    $('btnStop').textContent = t('stop');
    $('btnGps').textContent = state.gpsOn ? t('gpsOff') : t('gpsOn');
    const cur = state.languages.find(l => l.code === state.lang);
    $('btnLang').textContent = '🌐 ' + (cur ? cur.nativeName : t('change'));
  }

  function showOverlay() {
    const box = $('ovLangs');
    box.innerHTML = '';
    state.languages.forEach(l => {
      const b = document.createElement('button');
      b.type = 'button';
      b.textContent = l.nativeName + '  ·  ' + l.name;
      b.onclick = () => chooseLanguage(l.code);
      box.appendChild(b);
    });
    $('overlay').hidden = false;
  }

  async function chooseLanguage(code) {
    state.lang = code;
    localStorage.setItem('lang', code);
    $('overlay').hidden = true;
    stopPlayback();
    applyUiText();
    await loadPlaces();
  }

  // ---------- Bản đồ ----------
  function initMap() {
    map = L.map('map').setView([10.7769, 106.7009], 15);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);
    placeLayer = L.layerGroup().addTo(map);

    map.on('click', e => { if (state.demo) setPosition(e.latlng.lat, e.latlng.lng); });
  }

  function renderMarkers() {
    placeLayer.clearLayers();
    state.places.forEach(p => {
      L.circle([p.latitude, p.longitude], { radius: p.radiusMeters, color: '#c2410c', weight: 1, fillOpacity: 0.08 }).addTo(placeLayer);
      L.marker([p.latitude, p.longitude]).addTo(placeLayer)
        .bindTooltip(p.name)
        .on('click', () => play(p, 'manual'));
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
      btn.onclick = () => play(p, 'manual');

      li.append(meta, btn);
      list.appendChild(li);
    });
  }

  async function loadPlaces() {
    const q = new URLSearchParams({ lang: state.lang });
    state.places = await api('/places?' + q);
    refreshDistances();
    renderMarkers();
    renderList();
  }

  function refreshDistances() {
    state.places.forEach(p => {
      p.distanceMeters = state.pos ? distanceMeters(state.pos.lat, state.pos.lng, p.latitude, p.longitude) : Infinity;
    });
  }

  // ---------- GPS ----------
  function setPosition(lat, lng) {
    state.pos = { lat, lng };
    if (!userMarker) {
      userMarker = L.circleMarker([lat, lng], { radius: 9, color: '#fff', weight: 3, fillColor: '#2563eb', fillOpacity: 1 }).addTo(map);
    } else {
      userMarker.setLatLng([lat, lng]);
    }
    refreshDistances();
    renderList();
    evaluate();
  }

  function applyGpsMode() {
    if (state.watchId != null) {
      navigator.geolocation.clearWatch(state.watchId);
      state.watchId = null;
    }
    if (!state.gpsOn || state.demo) return;
    if (!navigator.geolocation) { setStatus(t('gpsDenied')); return; }
    setStatus(t('waiting'));
    state.watchId = navigator.geolocation.watchPosition(
      p => setPosition(p.coords.latitude, p.coords.longitude),
      () => setStatus(t('gpsDenied')),
      { enableHighAccuracy: true, maximumAge: 5000, timeout: 20000 });
  }

  /** Kiểm tra vị trí hiện tại: nếu đang trong bán kính một địa điểm → tự động thuyết minh. */
  function evaluate() {
    if (!state.pos || !state.gpsOn) return;
    const hit = state.places
      .filter(p => p.distanceMeters <= p.radiusMeters)
      .sort((a, b) => a.distanceMeters - b.distanceMeters)[0];

    setStatus(hit ? t('near') + ': ' + hit.name : t('none'));
    if (!hit || state.playing) return;

    const last = state.played.get(hit.id) || 0;
    if (Date.now() - last > COOLDOWN_MS) play(hit, 'gps');
  }

  // ---------- Phát thuyết minh ----------
  function play(place, trigger) {
    stopPlayback();
    const token = ++state.playToken;
    state.playing = true;
    state.played.set(place.id, Date.now());
    showPlayer(place);

    fetch(API + '/play-log', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ placeId: place.id, languageCode: place.languageCode, triggerType: trigger })
    }).catch(() => { /* ghi log lỗi không ảnh hưởng trải nghiệm */ });

    const done = () => {
      if (token !== state.playToken) return;   // lần phát này đã bị thay thế/dừng
      state.playing = false;
      $('player').hidden = true;
      evaluate();
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

  function pickVoice(locale) {
    const voices = window.speechSynthesis.getVoices();
    const norm = v => v.lang.replace('_', '-').toLowerCase();
    const l = locale.toLowerCase(), base = l.split('-')[0];
    return voices.find(v => norm(v) === l) || voices.find(v => norm(v).startsWith(base)) || null;
  }

  function speak(place, token, done) {
    if (!('speechSynthesis' in window)) { done(); return; }
    const synth = window.speechSynthesis;
    const voice = pickVoice(place.speechLocale);
    if (!voice && synth.getVoices().length > 0) $('plNote').textContent = t('noVoice');

    // Tách câu để tránh lỗi trình duyệt tự dừng khi đọc đoạn dài
    const parts = place.narrationText.split(/(?<=[.!?。！？])\s*/).map(s => s.trim()).filter(Boolean);
    parts.forEach((sentence, i) => {
      const u = new SpeechSynthesisUtterance(sentence);
      u.lang = place.speechLocale;
      if (voice) u.voice = voice;
      u.rate = 0.95;
      if (i === parts.length - 1) { u.onend = done; u.onerror = done; }
      synth.speak(u);
    });
  }

  function showPlayer(place) {
    $('plName').textContent = place.name;
    $('plNote').textContent = place.fellBack ? t('fallback') : '';
    $('plText').textContent = place.narrationText;
    $('player').hidden = false;
  }

  function stopPlayback() {
    state.playToken++;   // vô hiệu hóa mọi callback của lần phát trước
    state.playing = false;
    if (state.audio) { state.audio.pause(); state.audio = null; }
    if ('speechSynthesis' in window) window.speechSynthesis.cancel();
    $('player').hidden = true;
  }

  // ---------- Khởi tạo ----------
  function bindEvents() {
    $('btnLang').onclick = showOverlay;
    $('btnStop').onclick = stopPlayback;

    $('btnGps').onclick = () => {
      state.gpsOn = !state.gpsOn;
      if (state.gpsOn && 'speechSynthesis' in window) {
        // "mở khóa" speech synthesis bằng thao tác người dùng (chính sách autoplay của trình duyệt)
        window.speechSynthesis.speak(new SpeechSynthesisUtterance(''));
      }
      if (!state.gpsOn) { setStatus(''); stopPlayback(); }
      applyUiText();
      applyGpsMode();
      if (state.gpsOn) evaluate();
    };

    $('chkDemo').onchange = e => {
      state.demo = e.target.checked;
      applyGpsMode();
    };
  }

  async function init() {
    initMap();
    bindEvents();
    try {
      state.languages = await api('/languages');
    } catch {
      setStatus('Cannot reach server.');
      return;
    }
    if (!state.lang || !state.languages.some(l => l.code === state.lang)) {
      state.lang = 'vi';
      applyUiText();
      showOverlay();
    } else {
      applyUiText();
      await loadPlaces();
    }
  }

  init();
})();
