/* FitForge AI – Main JavaScript */

// ── Theme ──────────────────────────────────────────────────────
const THEME_KEY = 'fitforge-theme';
const LANG_KEY  = 'fitforge-lang';

function initTheme() {
  const saved = localStorage.getItem(THEME_KEY) || 'dark';
  document.documentElement.setAttribute('data-theme', saved);
  document.querySelectorAll('.theme-toggle').forEach(btn => {
    btn.setAttribute('data-theme', saved);
  });
}

function toggleTheme() {
  const current = document.documentElement.getAttribute('data-theme');
  const next = current === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', next);
  localStorage.setItem(THEME_KEY, next);
  document.querySelectorAll('.theme-toggle').forEach(btn => {
    btn.textContent = next === 'dark' ? '☀️ Light' : '🌙 Dark';
  });
}

// ── Mobile Sidebar ─────────────────────────────────────────────
function initSidebar() {
  const toggle = document.querySelector('.mobile-nav-toggle');
  const sidebar = document.querySelector('.sidebar');
  if (!toggle || !sidebar) return;
  toggle.addEventListener('click', () => sidebar.classList.toggle('open'));
  document.addEventListener('click', e => {
    if (!sidebar.contains(e.target) && !toggle.contains(e.target))
      sidebar.classList.remove('open');
  });
}

// ── Scroll Reveal ──────────────────────────────────────────────
function initScrollReveal() {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(e => { if (e.isIntersecting) e.target.classList.add('visible'); });
  }, { threshold: 0.1 });
  document.querySelectorAll('.reveal').forEach(el => observer.observe(el));
}

// ── Animated Counters ──────────────────────────────────────────
function animateCounter(el) {
  const target = parseInt(el.dataset.target || el.textContent, 10);
  if (isNaN(target)) return;
  let current = 0;
  const step = Math.max(1, Math.ceil(target / 60));
  const timer = setInterval(() => {
    current = Math.min(current + step, target);
    el.textContent = current.toLocaleString() + (el.dataset.suffix || '');
    if (current >= target) clearInterval(timer);
  }, 16);
}

function initCounters() {
  document.querySelectorAll('[data-counter]').forEach(el => {
    const obs = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) { animateCounter(el); obs.disconnect(); }
    });
    obs.observe(el);
  });
}

// ── Progress Rings ─────────────────────────────────────────────
function initProgressRings() {
  document.querySelectorAll('.progress-ring-svg').forEach(svg => {
    const circle = svg.querySelector('.ring-fill');
    if (!circle) return;
    const r = parseFloat(circle.getAttribute('r'));
    const circumference = 2 * Math.PI * r;
    const pct = parseFloat(svg.dataset.pct || 0);
    circle.style.strokeDasharray = circumference;
    circle.style.strokeDashoffset = circumference - (pct / 100) * circumference;
  });
}

// ── Progress Bar Animations ────────────────────────────────────
function initProgressBars() {
  document.querySelectorAll('.progress-bar-fill[data-pct]').forEach(bar => {
    setTimeout(() => { bar.style.width = bar.dataset.pct + '%'; }, 200);
  });
}

// ── Particles (Hero) ───────────────────────────────────────────
function initParticles() {
  const container = document.querySelector('.hero-particles');
  if (!container) return;
  for (let i = 0; i < 30; i++) {
    const p = document.createElement('div');
    p.className = 'particle';
    p.style.cssText = `
      left: ${Math.random() * 100}%;
      width: ${Math.random() * 3 + 1}px;
      height: ${Math.random() * 3 + 1}px;
      animation-duration: ${Math.random() * 8 + 6}s;
      animation-delay: ${Math.random() * 8}s;
      background: ${Math.random() > 0.5 ? '#2563eb' : '#22c55e'};
    `;
    container.appendChild(p);
  }
}

// ── Landing Stats Animate ──────────────────────────────────────
function initHeroStats() {
  document.querySelectorAll('.hero-stat-num[data-target]').forEach(el => {
    const obs = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) { animateCounter(el); obs.disconnect(); }
    });
    obs.observe(el);
  });
}

// ── Exercise Search Filter ─────────────────────────────────────
function initExerciseFilter() {
  const searchInput = document.getElementById('exercise-search');
  if (!searchInput) return;
  searchInput.addEventListener('input', () => {
    const q = searchInput.value.toLowerCase();
    document.querySelectorAll('.exercise-card').forEach(card => {
      const name = card.querySelector('.exercise-name')?.textContent.toLowerCase() || '';
      const muscles = card.querySelector('.exercise-muscles')?.textContent.toLowerCase() || '';
      card.style.display = (name.includes(q) || muscles.includes(q)) ? '' : 'none';
    });
  });
}

// ── Pace Calculator ────────────────────────────────────────────
function initPaceCalc() {
  const distInput = document.getElementById('pace-dist');
  const timeInput = document.getElementById('pace-time');
  const resultEl = document.getElementById('pace-result');
  if (!distInput || !timeInput || !resultEl) return;

  function calc() {
    const dist = parseFloat(distInput.value);
    const mins = parseFloat(timeInput.value);
    if (!dist || !mins || dist <= 0 || mins <= 0) { resultEl.textContent = '--:--'; return; }
    const pace = mins / dist;
    const m = Math.floor(pace);
    const s = Math.round((pace - m) * 60);
    resultEl.textContent = `${m}:${s.toString().padStart(2, '0')}`;
  }
  distInput.addEventListener('input', calc);
  timeInput.addEventListener('input', calc);
}

// ── Form Validation ─────────────────────────────────────────────
function initForms() {
  document.querySelectorAll('form[data-validate]').forEach(form => {
    form.addEventListener('submit', e => {
      let valid = true;
      form.querySelectorAll('[required]').forEach(input => {
        if (!input.value.trim()) {
          input.style.borderColor = '#ef4444';
          valid = false;
        } else {
          input.style.borderColor = '';
        }
      });
      if (!valid) e.preventDefault();
    });
  });
}

// ── Chart Bars ─────────────────────────────────────────────────
function initChartBars() {
  document.querySelectorAll('.chart-bar[data-height]').forEach(bar => {
    setTimeout(() => { bar.style.height = bar.dataset.height + 'px'; }, 300);
  });
}

// ── Language Switch ─────────────────────────────────────────────
function switchLang(lang) {
  localStorage.setItem(LANG_KEY, lang);
  const url = new URL(window.location.href);
  url.searchParams.set('lang', lang);
  window.location.href = url.toString();
}

// ── Toast Notification ──────────────────────────────────────────
function showToast(message, type = 'success') {
  const toast = document.createElement('div');
  toast.style.cssText = `
    position: fixed; bottom: 24px; right: 24px; z-index: 9999;
    background: ${type === 'success' ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)'};
    border: 1px solid ${type === 'success' ? 'rgba(34,197,94,0.4)' : 'rgba(239,68,68,0.4)'};
    color: ${type === 'success' ? '#86efac' : '#fca5a5'};
    padding: 14px 24px; border-radius: 12px;
    font-size: 14px; font-weight: 600;
    backdrop-filter: blur(20px);
    animation: fade-up 0.3s ease forwards;
    box-shadow: 0 8px 32px rgba(0,0,0,0.4);
  `;
  toast.textContent = message;
  document.body.appendChild(toast);
  setTimeout(() => toast.remove(), 3500);
}

// ── Workout Timer ───────────────────────────────────────────────
let timerInterval = null;
let timerSeconds = 0;

function startWorkoutTimer() {
  timerSeconds = 0;
  clearInterval(timerInterval);
  timerInterval = setInterval(() => {
    timerSeconds++;
    const h = Math.floor(timerSeconds / 3600);
    const m = Math.floor((timerSeconds % 3600) / 60);
    const s = timerSeconds % 60;
    const display = document.getElementById('workout-timer');
    if (display) display.textContent = `${h ? h+':' : ''}${m.toString().padStart(2,'0')}:${s.toString().padStart(2,'0')}`;
  }, 1000);
}

function stopWorkoutTimer() {
  clearInterval(timerInterval);
  return Math.round(timerSeconds / 60);
}

// ── Init ────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initSidebar();
  initScrollReveal();
  initCounters();
  initProgressRings();
  initProgressBars();
  initParticles();
  initHeroStats();
  initExerciseFilter();
  initPaceCalc();
  initForms();
  initChartBars();

  // Theme toggle buttons
  document.querySelectorAll('.theme-toggle').forEach(btn =>
    btn.addEventListener('click', toggleTheme));

  // Auto-show success toast
  const successMsg = document.querySelector('[data-toast]');
  if (successMsg) showToast(successMsg.dataset.toast);
});
