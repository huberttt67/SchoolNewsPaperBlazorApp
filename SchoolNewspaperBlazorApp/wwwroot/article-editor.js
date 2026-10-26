(function () {
    window.articleEditor = window.articleEditor || {};

    // Zapamiętujemy pozycję kursora, gdy użytkownik pracuje
    // w polu tekstowym i otwiera okno wyboru zdjęcia.
    var selections = Object.create(null);

    function rememberSelection(element) {
        if (!element || !element.id ||
            typeof element.selectionStart !== 'number') {
            return;
        }

        selections[element.id] = {
            start: element.selectionStart,
            end: element.selectionEnd
        };
    }

    document.addEventListener('click', function (event) {
        if (event.target && event.target.matches &&
            event.target.matches('textarea')) {
            rememberSelection(event.target);
        }
    }, true);

    document.addEventListener('keyup', function (event) {
        if (event.target && event.target.matches &&
            event.target.matches('textarea')) {
            rememberSelection(event.target);
        }
    }, true);

    document.addEventListener('select', function (event) {
        if (event.target && event.target.matches &&
            event.target.matches('textarea')) {
            rememberSelection(event.target);
        }
    }, true);

    document.addEventListener('input', function (event) {
        if (event.target && event.target.matches &&
            event.target.matches('textarea')) {
            rememberSelection(event.target);
        }
    }, true);

    window.articleEditor.insertAtCursor = function (elementId, textToInsert) {
        var element = document.getElementById(elementId);

        if (!element || typeof element.value !== 'string') {
            return null;
        }

        var current = element.value || '';
        var selection = selections[elementId];
        var isFocused = document.activeElement === element;

        var start = isFocused &&
            typeof element.selectionStart === 'number'
            ? element.selectionStart
            : (selection ? selection.start : current.length);

        var end = isFocused &&
            typeof element.selectionEnd === 'number'
            ? element.selectionEnd
            : (selection ? selection.end : current.length);

        start = Math.max(0, Math.min(start, current.length));
        end = Math.max(start, Math.min(end, current.length));

        var inserted = String(textToInsert || '');

        element.value =
            current.slice(0, start) +
            inserted +
            current.slice(end);

        var caret = start + inserted.length;

        element.focus();

        if (typeof element.setSelectionRange === 'function') {
            element.setSelectionRange(caret, caret);
        }

        rememberSelection(element);

        element.dispatchEvent(new Event('input', {
            bubbles: true
        }));

        return element.value;
    };
})();