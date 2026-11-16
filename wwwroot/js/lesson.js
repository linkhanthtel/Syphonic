// Lesson page: fills the thin bar under the navbar as the learner scrolls through the lesson text.
(function () {
    const body = document.querySelector('[data-lesson-body]');
    const bar = document.querySelector('[data-read-progress]');
    if (!body || !bar) return;

    function update() {
        const rect = body.getBoundingClientRect();
        // 0 when the top of the text reaches the top of the window, 1 when its end reaches the bottom.
        const distance = rect.height - window.innerHeight;
        const read = distance <= 0 ? (rect.bottom <= window.innerHeight ? 1 : 0) : -rect.top / distance;
        bar.style.width = Math.max(0, Math.min(1, read)) * 100 + '%';
    }

    window.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update);
    update();
})();
