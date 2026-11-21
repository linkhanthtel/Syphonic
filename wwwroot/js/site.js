// Client-side scripting for Syphonic (maps tooling, quizzes, etc.) expands from here.

// Practice tabs scroll sideways on narrow screens; make sure the current tab is the one in view.
(function () {
    const active = document.querySelector('.sy-tabs .nav-link.active');
    if (active) active.scrollIntoView({ block: 'nearest', inline: 'nearest' });
})();
