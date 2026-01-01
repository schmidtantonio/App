const formats = [
  { id: 'square', label: 'Quadrat (15x15)', icon: '▢' },
  { id: 'portrait', label: 'Hochformat (A6)', icon: '▣' },
  { id: 'landscape', label: 'Querformat (A6)', icon: '▭' },
  { id: 'panorama', label: 'Panorama (DL)', icon: '▭' },
];

const occasions = ['Geburtstag', 'Hochzeit', 'Dankeschön', 'Gute Besserung', 'Einladung'];
const styles = ['Aquarell (Künstlerisch)', 'Minimalistisch', 'Verspielt', 'Elegant', 'Modern'];

const backgrounds = [
  { style: 'Aquarell (Künstlerisch)', url: createPattern(['#f6d5f7', '#d0e6ff', '#fef3c7']), contrast: '#1b2336' },
  { style: 'Minimalistisch', url: gradient('#f8fafc', '#e2e8f0'), contrast: '#0f172a' },
  { style: 'Verspielt', url: confettiPattern(), contrast: '#111827' },
  { style: 'Elegant', url: gradient('#fdf3f0', '#f7e0d5'), contrast: '#1f2937' },
  { style: 'Modern', url: gradient('#e0f4ff', '#f8e7ff'), contrast: '#0f172a' },
];

function gradient(a, b) {
  return `linear-gradient(135deg, ${a}, ${b})`;
}

function confettiPattern() {
  const colors = ['#7c3aed', '#22c55e', '#f59e0b', '#3b82f6', '#ef4444'];
  const size = 320;
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = '#f8fafc';
  ctx.fillRect(0, 0, size, size);
  colors.forEach((c) => {
    ctx.fillStyle = c;
    for (let i = 0; i < 24; i++) {
      const x = Math.random() * size;
      const y = Math.random() * size;
      const w = 6 + Math.random() * 10;
      const h = 6 + Math.random() * 10;
      const r = Math.random() * Math.PI * 2;
      ctx.save();
      ctx.translate(x, y);
      ctx.rotate(r);
      ctx.fillRect(-w / 2, -h / 2, w, h);
      ctx.restore();
    }
  });
  return `url(${canvas.toDataURL()})`;
}

function createPattern(palette) {
  const size = 420;
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = palette[0];
  ctx.fillRect(0, 0, size, size);
  ctx.globalAlpha = 0.65;
  palette.slice(1).forEach((color, index) => {
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(120 + index * 120, 120 + index * 80, 140, 0, Math.PI * 2);
    ctx.fill();
  });
  return `url(${canvas.toDataURL()})`;
}

const els = {
  formatOptions: document.getElementById('formatOptions'),
  card: document.getElementById('card'),
  cardTitle: document.getElementById('cardTitle'),
  cardSubtitle: document.getElementById('cardSubtitle'),
  cardSignature: document.getElementById('cardSignature'),
  cardBackground: document.getElementById('cardBackground'),
  titleInput: document.getElementById('titleInput'),
  subtitleInput: document.getElementById('subtitleInput'),
  signatureInput: document.getElementById('signatureInput'),
  occasion: document.getElementById('occasion'),
  style: document.getElementById('style'),
  shuffleBackground: document.getElementById('shuffleBackground'),
  alignLeft: document.getElementById('alignLeft'),
  alignCenter: document.getElementById('alignCenter'),
  apiKeyInput: document.getElementById('apiKeyInput'),
  promptInput: document.getElementById('promptInput'),
  askGemini: document.getElementById('askGemini'),
  geminiStatus: document.getElementById('geminiStatus'),
  printCard: document.getElementById('printCard'),
};

function renderFormats(activeId) {
  els.formatOptions.innerHTML = '';
  formats.forEach((fmt) => {
    const div = document.createElement('div');
    div.className = `format-card ${activeId === fmt.id ? 'active' : ''}`;
    div.innerHTML = `<div class="preview">${fmt.icon}</div><div>${fmt.label}</div>`;
    div.addEventListener('click', () => setFormat(fmt.id));
    els.formatOptions.appendChild(div);
  });
}

function setFormat(id) {
  els.card.className = `card ${id}`;
  renderFormats(id);
}

function populateSelect(select, items) {
  select.innerHTML = items.map((i) => `<option value="${i}">${i}</option>`).join('');
}

function syncText() {
  els.cardTitle.textContent = els.titleInput.value || '—';
  els.cardSubtitle.textContent = els.subtitleInput.value || '';
  els.cardSignature.textContent = els.signatureInput.value;
  els.cardSignature.style.display = els.signatureInput.value ? 'block' : 'none';
}

function setBackground(styleName) {
  const candidates = backgrounds.filter((b) => b.style === styleName);
  const bg = candidates.length ? candidates[Math.floor(Math.random() * candidates.length)] : backgrounds[0];
  els.cardBackground.style.backgroundImage = bg.url;
  els.cardTitle.style.color = bg.contrast;
  els.cardSubtitle.style.color = bg.contrast;
  els.cardSignature.style.color = bg.contrast;
}

function saveApiKey() {
  localStorage.setItem('geminiApiKey', els.apiKeyInput.value.trim());
}

function loadApiKey() {
  const key = localStorage.getItem('geminiApiKey');
  if (key) els.apiKeyInput.value = key;
}

async function askGemini() {
  const apiKey = els.apiKeyInput.value.trim();
  if (!apiKey) {
    setStatus('Bitte API Key eintragen, um Gemini zu nutzen.', 'warn');
    return;
  }
  saveApiKey();
  setStatus('Gemini denkt nach…', 'info');
  els.askGemini.disabled = true;

  const prompt = buildPrompt();

  try {
    const res = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=${apiKey}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        contents: [{
          parts: [{ text: prompt }],
        }],
      }),
    });

    if (!res.ok) {
      throw new Error(`${res.status} – ${res.statusText}`);
    }
    const data = await res.json();
    const text = data?.candidates?.[0]?.content?.parts?.[0]?.text ?? '';
    if (!text) throw new Error('Keine Antwort von Gemini erhalten.');

    const { headline, message, signature, styleSuggestion } = parseGeminiResponse(text);
    if (headline) els.titleInput.value = headline;
    if (message) els.subtitleInput.value = message;
    if (signature) els.signatureInput.value = signature;
    if (styleSuggestion) els.style.value = styleSuggestion;
    syncText();
    setBackground(els.style.value);
    setStatus('Text von Gemini eingefügt.', 'success');
  } catch (err) {
    console.error(err);
    setStatus(`Gemini-Fehler: ${err.message}`, 'error');
  } finally {
    els.askGemini.disabled = false;
  }
}

function buildPrompt() {
  const custom = els.promptInput.value.trim();
  const base = `Erstelle einen kurzen Grußkartentext auf Deutsch. Anlass: ${els.occasion.value}. Stil: ${els.style.value}. ` +
    `Gib ein JSON-Objekt mit den Feldern headline, message, signature (optional) und styleSuggestion zurück.`;
  return custom ? `${base} Benutzerwunsch: ${custom}` : base;
}

function parseGeminiResponse(raw) {
  try {
    const jsonStart = raw.indexOf('{');
    if (jsonStart >= 0) {
      const snippet = raw.slice(jsonStart);
      return JSON.parse(snippet);
    }
  } catch (e) {
    console.warn('Konnte JSON nicht parsen, verwende Rohtext');
  }
  return { message: raw };
}

function setStatus(text, variant = 'info') {
  const colors = { info: '#475467', success: '#16a34a', warn: '#f59e0b', error: '#dc2626' };
  els.geminiStatus.style.color = colors[variant] || colors.info;
  els.geminiStatus.textContent = text;
}

function init() {
  renderFormats('panorama');
  populateSelect(els.occasion, occasions);
  populateSelect(els.style, styles);
  els.style.value = 'Aquarell (Künstlerisch)';
  setBackground(els.style.value);
  syncText();
  loadApiKey();

  els.titleInput.addEventListener('input', syncText);
  els.subtitleInput.addEventListener('input', syncText);
  els.signatureInput.addEventListener('input', syncText);
  els.occasion.addEventListener('change', () => setStatus('Anlass geändert – du kannst Gemini fragen.', 'info'));
  els.style.addEventListener('change', () => setBackground(els.style.value));
  els.shuffleBackground.addEventListener('click', () => setBackground(els.style.value));
  els.alignLeft.addEventListener('click', () => {
    els.cardContent.style.textAlign = 'left';
  });
  els.alignCenter.addEventListener('click', () => {
    const current = els.cardContent.style.textAlign || 'left';
    els.cardContent.style.textAlign = current === 'center' ? 'left' : 'center';
  });
  els.askGemini.addEventListener('click', askGemini);
  els.apiKeyInput.addEventListener('blur', saveApiKey);
  els.printCard.addEventListener('click', () => window.print());
}

document.addEventListener('DOMContentLoaded', init);
