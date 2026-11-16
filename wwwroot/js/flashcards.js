// Flashcards: shows one card at a time from the JSON the page embeds.
// Nothing is sent to the server; the tally lives only in this page.
(function () {
    const root = document.getElementById('flashcards');
    const dataElement = document.getElementById('flashcards-data');
    if (!root || !dataElement) return;

    const allCards = JSON.parse(dataElement.textContent);
    if (!allCards.length) return;

    const find = name => root.querySelector('[data-flash-' + name + ']');
    const study = find('study');
    const done = find('done');
    const card = find('card');
    const side = find('side');
    const text = find('text');
    const explanation = find('explanation');
    const lesson = find('lesson');
    const position = find('position');
    const tally = find('tally');
    const progress = find('progress');
    const frontActions = find('front-actions');
    const backActions = find('back-actions');
    const retry = find('retry');

    let deck = [];      // cards in this pass
    let index = 0;      // card being shown
    let flipped = false;
    let known = 0;
    let missed = [];    // cards marked "Still learning" in this pass

    function shuffle(cards) {
        const copy = cards.slice();
        for (let i = copy.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [copy[i], copy[j]] = [copy[j], copy[i]];
        }
        return copy;
    }

    function start(cards) {
        deck = shuffle(cards);
        index = 0;
        known = 0;
        missed = [];
        done.hidden = true;
        study.hidden = false;
        show();
    }

    function show() {
        const current = deck[index];
        side.textContent = flipped ? 'Answer' : 'Question';
        text.textContent = flipped ? current.answer : current.question;
        explanation.textContent = current.explanation || '';
        explanation.hidden = !flipped || !current.explanation;
        lesson.textContent = 'From ' + current.lesson;
        card.classList.toggle('sy-flash-card-back', flipped);
        card.setAttribute('aria-label', (flipped ? 'Answer: ' + current.answer : 'Question: ' + current.question) + '. Select to turn the card.');

        position.textContent = 'Card ' + (index + 1) + ' of ' + deck.length;
        tally.textContent = known + ' known, ' + missed.length + ' still learning';
        progress.style.width = (index / deck.length * 100) + '%';

        frontActions.hidden = flipped;
        backActions.hidden = !flipped;
        find('prev').disabled = index === 0;
    }

    function flip() {
        flipped = !flipped;
        show();
    }

    function move(step) {
        const next = index + step;
        if (next < 0) return;
        flipped = false;
        if (next >= deck.length) {
            finish();
            return;
        }
        index = next;
        show();
    }

    function mark(result) {
        if (result === 'known') known++;
        else missed.push(deck[index]);
        move(1);
    }

    function finish() {
        study.hidden = true;
        done.hidden = false;
        const title = find('done-title');
        title.textContent = 'Deck finished: you knew ' + known + ' of ' + deck.length;
        find('done-text').textContent = missed.length
            ? 'Go through the ' + (missed.length === 1 ? 'card' : missed.length + ' cards') + ' you are still learning once more before you move on.'
            : 'Try the quiz next to see whether the answers stick without the cards.';
        retry.hidden = missed.length === 0;
        title.focus();
    }

    card.addEventListener('click', flip);
    find('flip').addEventListener('click', function () { flip(); card.focus(); });
    find('prev').addEventListener('click', function () { move(-1); });
    find('next').addEventListener('click', function () { move(1); });
    root.querySelectorAll('[data-flash-mark]').forEach(function (button) {
        button.addEventListener('click', function () {
            mark(button.dataset.flashMark);
            if (!study.hidden) card.focus();
        });
    });
    retry.addEventListener('click', function () { start(missed); card.focus(); });
    find('restart').addEventListener('click', function () { start(allCards); card.focus(); });

    document.addEventListener('keydown', function (event) {
        if (study.hidden || event.altKey || event.ctrlKey || event.metaKey) return;
        const target = event.target instanceof Element ? event.target : null;
        // Leave typing and the deck picker alone.
        if (target && target.closest('input, select, textarea')) return;

        if (event.key === 'ArrowRight') { move(1); event.preventDefault(); }
        else if (event.key === 'ArrowLeft') { move(-1); event.preventDefault(); }
        // Space on a button or link already activates it, so only handle it elsewhere.
        else if (event.key === ' ' && !(target && target.closest('button, a'))) { flip(); event.preventDefault(); }
    });

    start(allCards);
})();
