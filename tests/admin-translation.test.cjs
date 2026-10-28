const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

test('new place: translate, preserve drafts, retry errors, cancel, then save all', async () => {
  const node = (value = '') => ({ value, disabled: false, addEventListener() {}, removeEventListener() {} });
  const field = (code, name = '', text = '') => {
    const inputs = Object.fromEntries(['.tName', '.tText', '.tAudio', '.tSave', '.tTest', '.tAudioFile'].map(s => [s, node()]));
    inputs['.tName'].value = name;
    inputs['.tText'].value = text;
    return { dataset: { lang: code }, querySelector: s => inputs[s] || null };
  };
  const fields = [field('vi', 'Tên Việt', 'Thuyết minh Việt'), field('en'), field('ja'), field('ko', 'Manual draft'), field('fr')];
  const nodes = new Map();
  const get = s => { if (!nodes.has(s)) nodes.set(s, node()); return nodes.get(s); };
  Object.entries({ '#fSlug': 'new-place', '#fLat': '10.7', '#fLng': '106.7', '#fRadius': '100', '#fAddress': 'Address' })
    .forEach(([s, v]) => { get(s).value = v; });
  get('#fActive').checked = true;
  const saveButtons = [node(), node()];
  const dlg = get('#dlg');
  dlg.showModal = () => { dlg.open = true; };
  const controls = [...nodes.values(), ...saveButtons, ...fields.flatMap(f => ['.tName', '.tText', '.tAudio'].map(s => f.querySelector(s)))];
  dlg.querySelectorAll = () => controls;
  const document = {
    querySelector: s => s === '#transBox fieldset[data-lang="vi"]' ? fields[0] : get(s),
    querySelectorAll: s => s === '#transBox fieldset' ? fields : s === '.btnSaveAll' ? saveButtons : [],
    createElement: () => ({ remove() {} }), body: { appendChild() {} }
  };
  let japaneseMode = 'fail';
  const requests = [], stored = [];
  const response = data => ({ ok: true, status: 200, json: async () => data });
  const context = vm.createContext({ document, AbortController, sessionStorage: { getItem: () => null }, setTimeout() {},
    fetch: async (url, options) => {
      const body = options.body ? JSON.parse(options.body) : null;
      requests.push({ url, method: options.method, body });
      if (url.endsWith('/auto-translate')) {
        assert.ok(controls.every(c => c.disabled));
        assert.equal(body.name, 'Tên Việt');
        if (body.targetLanguage === 'ja' && japaneseMode === 'fail') return { ok: false, status: 400, json: async () => ({ message: 'Ollama unavailable' }) };
        if (body.targetLanguage === 'ja' && japaneseMode === 'cancel') {
          get('#btnStopTranslate').onclick();
          assert.ok(options.signal.aborted);
          throw new Error('Aborted');
        }
        return response({ name: 'Translated', narrationText: 'Translated narration' });
      }
      if (url === '/api/admin/places' && options.method === 'POST') {
        const place = { ...body, id: 7, translations: [] };
        stored.push(place);
        return response(place);
      }
      if (url.includes('/translations/')) {
        stored[0].translations.push({ ...body, languageCode: url.split('/').pop() });
        return response(stored[0]);
      }
      if (url === '/api/admin/places') return response(stored);
      throw new Error('Unexpected request ' + url);
    }
  });
  const source = fs.readFileSync(path.join(__dirname, '../src/AudioGuide.API/wwwroot/admin/admin.js'), 'utf8');
  vm.runInContext(source.replace('if (token) start().catch(() => logout()); else showLogin();',
    'globalThis.setup = langs => { languages = langs; token = "token"; openPlaceDialog(null); };'), context);
  context.setup(fields.map(f => ({ code: f.dataset.lang, isActive: f.dataset.lang !== 'fr', nativeName: f.dataset.lang })));
  assert.match(dlg.innerHTML, /btnAutoTranslate/);
  assert.match(dlg.innerHTML, /fieldset data-lang="vi"/);
  await get('#btnAutoTranslate').onclick();
  assert.deepEqual(requests.map(r => r.body.targetLanguage), ['en', 'ja']);
  assert.equal(stored.length, 0);
  assert.equal(fields[3].querySelector('.tName').value, 'Manual draft');
  assert.match(get('#translationProgress').textContent, /Ollama unavailable/);
  assert.ok(controls.every(c => !c.disabled));
  japaneseMode = 'cancel';
  await get('#btnAutoTranslate').onclick();
  assert.match(get('#translationProgress').textContent, /Đã dừng/);
  assert.equal(fields[2].querySelector('.tName').value, '');
  japaneseMode = 'success';
  await get('#btnAutoTranslate').onclick();
  assert.deepEqual(requests.map(r => r.body.targetLanguage), ['en', 'ja', 'ja', 'ja']);
  await saveButtons[0].onclick();
  assert.equal(stored.length, 1);
  assert.equal(stored[0].slug, 'new-place');
  assert.deepEqual(stored[0].translations.map(t => t.languageCode), ['vi', 'en', 'ja']);
  assert.equal(stored[0].translations[0].narrationText, 'Thuyết minh Việt');
});
