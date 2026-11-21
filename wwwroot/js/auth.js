// Log in and sign-up forms: the "Show password" tick box switches password fields to plain text and back.
(function () {
    const toggle = document.querySelector('[data-show-passwords]');
    if (!toggle) return;

    const fields = toggle.closest('form').querySelectorAll('input[autocomplete$="password"]');
    toggle.addEventListener('change', function () {
        fields.forEach(function (field) { field.type = toggle.checked ? 'text' : 'password'; });
    });
})();
