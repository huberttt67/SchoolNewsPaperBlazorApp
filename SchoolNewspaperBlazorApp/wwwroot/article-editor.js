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

    // Zmienia ułożenie (left / center / right) zdjęcia, przy którym stoi
    // kursor albo które jest zaznaczone w polu tekstowym. Kursor może stać
    // w znaczniku [[IMG:...]] albo tuż za nim (np. zaraz po wstawieniu).
    // Zwraca nowy tekst albo null, gdy przy kursorze nie ma zdjęcia.
    window.articleEditor.setAlignAtCursor = function (elementId, align) {
        var element = document.getElementById(elementId);

        if (!element || typeof element.value !== 'string' ||
            !/^(left|center|right)$/.test(align)) {
            return null;
        }

        var current = element.value || '';
        var selection = selections[elementId];

        var start = selection ? selection.start :
            (typeof element.selectionStart === 'number'
                ? element.selectionStart : current.length);
        var end = selection ? selection.end :
            (typeof element.selectionEnd === 'number'
                ? element.selectionEnd : start);

        var pattern = /\[\[IMG:([A-Za-z0-9._-]+)(?:\|(?:left|right|center))?\]\]/gi;
        var match;
        var target = null;

        while ((match = pattern.exec(current)) !== null) {
            var markerStart = match.index;
            var markerEnd = match.index + match[0].length;

            // Kursor (lub zaznaczenie) dotyka znacznika.
            var touches = start <= markerEnd && end >= markerStart;

            // Kursor stoi w pustych liniach tuż za znacznikiem.
            var gap = current.slice(markerEnd, start);
            var justAfter = start > markerEnd && gap.trim() === '';

            if (touches || justAfter) {
                target = {
                    start: markerStart,
                    end: markerEnd,
                    file: match[1]
                };

                // Dotknięty znacznik wygrywa z „tuż za”.
                if (touches) {
                    break;
                }
            }
        }

        if (!target) {
            return null;
        }

        var replacement = '[[IMG:' + target.file + '|' + align + ']]';

        element.value =
            current.slice(0, target.start) +
            replacement +
            current.slice(target.end);

        var caret = target.start + replacement.length;

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

// Płynne przewinięcie strony do elementu (np. do formularza edycji).
window.scrollToElementById = function (id) {
    var element = document.getElementById(id);

    if (element && typeof element.scrollIntoView === 'function') {
        element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};