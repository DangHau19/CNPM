(() => {
  'use strict';

  const $ = s => document.querySelector(s);
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  let token = sessionStorage.getItem('adminToken');
  let languages = [];
  let places = [];
  let tab = 'places';

  // ---------- HTTP ----------
  async function api(path, method = 'GET', body) {
    const res = await fetch('/api/admin' + path, {
      method,
      headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: 'Bearer ' + token } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body)
    });
    if (res.status === 401 && path !== '/auth/login') { logout(); throw new Error('Phiên đăng nhập đã hết hạn.'); }
    const data = res.status === 204 ? null : await res.json().catch(() => null);
    if (!res.ok) throw new Error((data && (data.message || data.title)) || 'Lỗi ' + res.status);
    return data;
  }

  function toast(msg, isError) {
    const el = document.createElement('div');
    el.className = 'toast' + (isError ? ' err' : '');
    el.textContent = msg;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 3200);
  }

  // Bọc handler: bắt lỗi và hiển thị toast
  const guard = fn => async (...args) => {
    try { await fn(...args); } catch (e) { toast(e.message, true); }
  };

  // ---------- Đăng nhập ----------
  function showLogin() { $('#loginView').hidden = false; $('#appView').hidden = true; }

  function logout() {
    token = null;
    sessionStorage.removeItem('adminToken');
    showLogin();
  }

  $('#btnLogin').onclick = guard(async () => {
    const r = await api('/auth/login', 'POST', { username: $('#lgUser').value, password: $('#lgPass').value });
    token = r.token;
    sessionStorage.setItem('adminToken', token);
    await start();
  });
  $('#lgPass').addEventListener('keydown', e => { if (e.key === 'Enter') $('#btnLogin').click(); });
  $('#btnLogout').onclick = logout;

  document.querySelectorAll('nav [data-tab]').forEach(b => {
    b.onclick = guard(async () => { tab = b.dataset.tab; await render(); });
  });

  async function start() {
    $('#loginView').hidden = true;
    $('#appView').hidden = false;
    await render();
  }

  async function render() {
    document.querySelectorAll('nav [data-tab]').forEach(b => b.classList.toggle('active', b.dataset.tab === tab));
    languages = await api('/languages');
    if (tab === 'places') await renderPlaces();
    else if (tab === 'languages') renderLanguages();
    else await renderStats();
  }

  // ---------- Địa điểm ----------
  async function renderPlaces() {
    places = await api('/places');
    const viName = p => (p.translations.find(t => t.languageCode === 'vi') || p.translations[0] || {}).name || p.slug;
    $('#view').innerHTML = `
      <div class="bar"><h2>Địa điểm (${places.length})</h2><button class="primary" id="btnAddPlace" type="button">+ Thêm địa điểm</button></div>
      <table>
        <thead><tr><th>#</th><th>Tên</th><th>Tọa độ</th><th>Bán kính</th><th>Bản dịch</th><th>Trạng thái</th><th></th></tr></thead>
        <tbody>${places.map(p => `
          <tr>
            <td>${p.id}</td>
            <td><strong>${esc(viName(p))}</strong><br><span class="muted">${esc(p.slug)}</span></td>
            <td>${p.latitude.toFixed(5)}, ${p.longitude.toFixed(5)}</td>
            <td>${p.radiusMeters} m</td>
            <td>${p.translations.map(t => esc(t.languageCode)).join(', ') || '—'}</td>
            <td>${p.isActive ? 'Hiển thị' : '<span class="muted">Ẩn</span>'}</td>
            <td><button data-edit="${p.id}" type="button">Sửa</button>
                <button data-del="${p.id}" class="danger" type="button">Xóa</button></td>
          </tr>`).join('')}
        </tbody>
      </table>`;
    $('#btnAddPlace').onclick = () => openPlaceDialog(null);
    document.querySelectorAll('[data-edit]').forEach(b => b.onclick = () => openPlaceDialog(+b.dataset.edit));
    document.querySelectorAll('[data-del]').forEach(b => b.onclick = guard(async () => {
      if (!confirm('Xóa địa điểm này cùng toàn bộ bản dịch và lịch sử nghe?')) return;
      await api('/places/' + b.dataset.del, 'DELETE');
      toast('Đã xóa địa điểm.');
      await renderPlaces();
    }));
  }

  function openPlaceDialog(id) {
    const p = id ? places.find(x => x.id === id) : { slug: '', latitude: 10.7769, longitude: 106.7009, radiusMeters: 100, address: '', imageUrl: '', isActive: true, translations: [] };
    const dlg = $('#dlg');
    const activeLangs = languages.filter(l => l.isActive || p.translations.some(t => t.languageCode === l.code));

    dlg.innerHTML = `
      <h3>${id ? 'Sửa địa điểm #' + id : 'Thêm địa điểm'}</h3>
      <div class="card">
        <div class="grid">
          <div><label>Slug</label><input id="fSlug" value="${esc(p.slug)}" placeholder="dinh-doc-lap"></div>
          <div><label>Vĩ độ</label><input id="fLat" type="number" step="0.00001" value="${p.latitude}"></div>
          <div><label>Kinh độ</label><input id="fLng" type="number" step="0.00001" value="${p.longitude}"></div>
          <div><label>Bán kính (m)</label><input id="fRadius" type="number" value="${p.radiusMeters}"></div>
        </div>
        <label>Địa chỉ</label><input id="fAddress" value="${esc(p.address)}">
        <label>Ảnh (URL, tùy chọn)</label><input id="fImage" value="${esc(p.imageUrl || '')}">
        <label><input type="checkbox" id="fActive" ${p.isActive ? 'checked' : ''} style="width:auto"> Hiển thị cho du khách</label>
        <div class="row"><button class="primary" id="btnSavePlace" type="button">Lưu thông tin địa điểm</button></div>
      </div>
      <div id="transBox">
        ${id ? activeLangs.map(l => translationBlock(l, p.translations.find(t => t.languageCode === l.code))).join('')
             : '<p class="muted">Lưu địa điểm trước, sau đó nhập nội dung thuyết minh cho từng ngôn ngữ.</p>'}
      </div>
      <div class="row"><button id="btnCloseDlg" type="button">Đóng</button></div>`;

    $('#btnCloseDlg').onclick = () => dlg.close();
    $('#btnSavePlace').onclick = guard(async () => {
      const body = {
        slug: $('#fSlug').value, latitude: parseFloat($('#fLat').value), longitude: parseFloat($('#fLng').value),
        radiusMeters: parseInt($('#fRadius').value, 10), address: $('#fAddress').value,
        imageUrl: $('#fImage').value, isActive: $('#fActive').checked
      };
      const saved = id ? await api('/places/' + id, 'PUT', body) : await api('/places', 'POST', body);
      toast('Đã lưu địa điểm.');
      await renderPlaces();
      openPlaceDialog(saved.id);   // mở lại để nhập bản dịch
    });

    if (id) bindTranslationButtons(id, activeLangs);
    if (!dlg.open) dlg.showModal();
  }

  function translationBlock(lang, t) {
    return `
      <fieldset data-lang="${esc(lang.code)}">
        <legend>${esc(lang.nativeName)} (${esc(lang.code)})</legend>
        <label>Tên địa điểm</label><input class="tName" value="${esc(t?.name || '')}">
        <label>Nội dung thuyết minh</label><textarea class="tText">${esc(t?.narrationText || '')}</textarea>
        <label>File âm thanh (URL .mp3, tùy chọn – bỏ trống để dùng giọng đọc tự động)</label><input class="tAudio" value="${esc(t?.audioUrl || '')}">
        <div class="row">
          <button class="primary tSave" type="button">Lưu bản dịch</button>
          <button class="tTest" type="button">🔊 Nghe thử</button>
          ${t ? '<button class="danger tDel" type="button">Xóa bản dịch</button>' : ''}
        </div>
      </fieldset>`;
  }

  function bindTranslationButtons(placeId, langs) {
    document.querySelectorAll('#transBox fieldset').forEach(fs => {
      const code = fs.dataset.lang;
      const lang = langs.find(l => l.code === code);
      fs.querySelector('.tSave').onclick = guard(async () => {
        await api(`/places/${placeId}/translations/${code}`, 'PUT', {
          name: fs.querySelector('.tName').value,
          narrationText: fs.querySelector('.tText').value,
          audioUrl: fs.querySelector('.tAudio').value
        });
        toast('Đã lưu bản dịch ' + code + '.');
        await renderPlaces();
        openPlaceDialog(placeId);
      });
      fs.querySelector('.tTest').onclick = () => {
        const text = fs.querySelector('.tText').value;
        if (!text || !('speechSynthesis' in window)) return;
        window.speechSynthesis.cancel();
        const u = new SpeechSynthesisUtterance(text);
        u.lang = lang.speechLocale;
        window.speechSynthesis.speak(u);
      };
      const del = fs.querySelector('.tDel');
      if (del) del.onclick = guard(async () => {
        if (!confirm('Xóa bản dịch ' + code + '?')) return;
        await api(`/places/${placeId}/translations/${code}`, 'DELETE');
        toast('Đã xóa bản dịch.');
        await renderPlaces();
        openPlaceDialog(placeId);
      });
    });
  }

  // ---------- Ngôn ngữ ----------
  function renderLanguages() {
    $('#view').innerHTML = `
      <h2>Ngôn ngữ</h2>
      <table>
        <thead><tr><th>Mã</th><th>Tên</th><th>Tên bản ngữ</th><th>Giọng đọc (locale)</th><th>Thứ tự</th><th>Kích hoạt</th><th></th></tr></thead>
        <tbody>${languages.map(l => `
          <tr data-code="${esc(l.code)}">
            <td><strong>${esc(l.code)}</strong></td>
            <td><input class="lName" value="${esc(l.name)}"></td>
            <td><input class="lNative" value="${esc(l.nativeName)}"></td>
            <td><input class="lLocale" value="${esc(l.speechLocale)}"></td>
            <td><input class="lOrder" type="number" value="${l.sortOrder}" style="width:70px"></td>
            <td><input class="lActive" type="checkbox" ${l.isActive ? 'checked' : ''} style="width:auto"></td>
            <td><button class="lSave primary" type="button">Lưu</button></td>
          </tr>`).join('')}
        </tbody>
      </table>
      <div class="card" style="margin-top:16px">
        <h3>Thêm ngôn ngữ</h3>
        <div class="grid">
          <div><label>Mã (vd: fr)</label><input id="nCode"></div>
          <div><label>Tên (English)</label><input id="nName"></div>
          <div><label>Tên bản ngữ</label><input id="nNative"></div>
          <div><label>Locale giọng đọc (vd: fr-FR)</label><input id="nLocale"></div>
        </div>
        <div class="row"><button class="primary" id="btnAddLang" type="button">+ Thêm</button></div>
      </div>`;

    document.querySelectorAll('tr[data-code]').forEach(tr => {
      tr.querySelector('.lSave').onclick = guard(async () => {
        await api('/languages/' + tr.dataset.code, 'PUT', {
          code: tr.dataset.code, name: tr.querySelector('.lName').value, nativeName: tr.querySelector('.lNative').value,
          speechLocale: tr.querySelector('.lLocale').value, isActive: tr.querySelector('.lActive').checked,
          sortOrder: parseInt(tr.querySelector('.lOrder').value, 10) || 0
        });
        toast('Đã lưu ngôn ngữ.');
        await render();
      });
    });

    $('#btnAddLang').onclick = guard(async () => {
      await api('/languages', 'POST', {
        code: $('#nCode').value, name: $('#nName').value, nativeName: $('#nNative').value,
        speechLocale: $('#nLocale').value, isActive: true, sortOrder: languages.length + 1
      });
      toast('Đã thêm ngôn ngữ.');
      await render();
    });
  }

  // ---------- Thống kê ----------
  async function renderStats(days = 30) {
    const s = await api('/stats?days=' + days);
    const max = Math.max(1, ...s.topPlaces.map(x => x.count));
    $('#view').innerHTML = `
      <div class="bar"><h2>Thống kê lượt nghe</h2>
        <select id="statDays" style="width:auto">
          ${[7, 30, 90, 365].map(d => `<option value="${d}" ${d === days ? 'selected' : ''}>${d} ngày qua</option>`).join('')}
        </select></div>
      <div class="grid">
        <div class="card"><div class="muted">Tổng lượt nghe</div><h2>${s.totalPlays}</h2></div>
        <div class="card"><div class="muted">Tự động (GPS)</div><h2>${s.gpsPlays}</h2></div>
        <div class="card"><div class="muted">Thủ công</div><h2>${s.manualPlays}</h2></div>
      </div>
      <div class="card"><h3>Địa điểm được nghe nhiều nhất</h3>
        ${s.topPlaces.map(x => `<div style="margin:8px 0"><div>${esc(x.label)} — <strong>${x.count}</strong></div>
          <div class="bar-fill" style="width:${Math.round(x.count / max * 100)}%"></div></div>`).join('') || '<p class="muted">Chưa có dữ liệu.</p>'}
      </div>
      <div class="card"><h3>Theo ngôn ngữ</h3>
        ${s.byLanguage.map(x => `<div style="margin:6px 0">${esc(x.label)} — <strong>${x.count}</strong></div>`).join('') || '<p class="muted">Chưa có dữ liệu.</p>'}
      </div>`;
    $('#statDays').onchange = guard(async e => { await renderStats(parseInt(e.target.value, 10)); });
  }

  // ---------- Khởi động ----------
  if (token) start().catch(() => logout()); else showLogin();
})();
