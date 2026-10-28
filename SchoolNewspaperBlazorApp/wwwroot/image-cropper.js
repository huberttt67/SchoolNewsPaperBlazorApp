(function () {
    'use strict';

    window.imageCropper = window.imageCropper || {};

    var STAGE_MAX_W = 560;
    var STAGE_MAX_H = 380;
    var OUT_MAX = 1600;      // dłuższy bok zapisanego zdjęcia (px)
    var THUMB_W = 360;
    var THUMB_H = 270;

    var st = null;

    function el(id) { return document.getElementById(id); }
    function clamp(v, a, b) { return Math.max(a, Math.min(b, v)); }

    function currentAspect() {
        return st.aspect > 0 ? st.aspect : st.img.naturalWidth / st.img.naturalHeight;
    }

    // Położenie zdjęcia w kadrze: powiększenie 100% = zdjęcie wypełnia kadr.
    function layout() {
        var iw = st.img.naturalWidth, ih = st.img.naturalHeight;
        var base = Math.max(st.w / iw, st.h / ih);
        var scale = base * st.zoom;
        var dw = iw * scale, dh = ih * scale;
        var maxX = Math.max(0, (dw - st.w) / 2);
        var maxY = Math.max(0, (dh - st.h) / 2);

        return {
            scale: scale,
            maxX: maxX,
            maxY: maxY,
            left: (st.w - dw) / 2 - st.panX * maxX,
            top: (st.h - dh) / 2 - st.panY * maxY,
            dw: dw,
            dh: dh
        };
    }

    function sourceRect() {
        var L = layout();
        var iw = st.img.naturalWidth, ih = st.img.naturalHeight;
        var sw = Math.min(iw, st.w / L.scale);
        var sh = Math.min(ih, st.h / L.scale);
        var sx = clamp(-L.left / L.scale, 0, iw - sw);
        var sy = clamp(-L.top / L.scale, 0, ih - sh);
        return { sx: sx, sy: sy, sw: sw, sh: sh };
    }

    function outputSize(maxW, maxH) {
        var r = sourceRect();
        var ratio = Math.min(1, maxW / r.sw, maxH / r.sh);
        return {
            w: Math.max(1, Math.round(r.sw * ratio)),
            h: Math.max(1, Math.round(r.sh * ratio))
        };
    }

    function draw() {
        if (!st || !st.ready) { return; }

        var L = layout();
        var ctx = st.ctx;

        ctx.setTransform(st.dpr, 0, 0, st.dpr, 0, 0);
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, st.w, st.h);
        ctx.imageSmoothingQuality = 'high';
        ctx.drawImage(st.img, L.left, L.top, L.dw, L.dh);

        // Siatka trójpodziału ułatwia ustawienie kadru.
        ctx.strokeStyle = 'rgba(255,255,255,.55)';
        ctx.lineWidth = 1;
        ctx.beginPath();
        for (var i = 1; i < 3; i++) {
            ctx.moveTo(Math.round(st.w * i / 3) + .5, 0);
            ctx.lineTo(Math.round(st.w * i / 3) + .5, st.h);
            ctx.moveTo(0, Math.round(st.h * i / 3) + .5);
            ctx.lineTo(st.w, Math.round(st.h * i / 3) + .5);
        }
        ctx.stroke();

        syncControls(L);
    }

    function syncControls(L) {
        var zoom = el('icZoom'), px = el('icPanX'), py = el('icPanY');
        if (zoom) { zoom.value = Math.round(st.zoom * 100); }
        if (px) { px.value = Math.round(st.panX * 100); px.disabled = L.maxX < 1; }
        if (py) { py.value = Math.round(st.panY * 100); py.disabled = L.maxY < 1; }

        var zv = el('icZoomVal'), pxv = el('icPanXVal'), pyv = el('icPanYVal');
        if (zv) { zv.textContent = Math.round(st.zoom * 100) + '%'; }
        if (pxv) { pxv.textContent = Math.round(st.panX * 100); }
        if (pyv) { pyv.textContent = Math.round(st.panY * 100); }

        var info = el('icInfo');
        if (info) {
            var o = outputSize(OUT_MAX, OUT_MAX);
            info.textContent = 'Wynik: ' + o.w + ' × ' + o.h + ' px';
        }
    }

    function resizeStage() {
        var holder = el('icStageHolder');
        var canvas = el('icCanvas');
        if (!holder || !canvas || !st || !st.ready) { return; }

        var aspect = currentAspect();
        var availW = Math.max(160, Math.min(STAGE_MAX_W, holder.clientWidth || STAGE_MAX_W));
        var w = availW;
        var h = w / aspect;

        if (h > STAGE_MAX_H) {
            h = STAGE_MAX_H;
            w = h * aspect;
        }

        w = Math.round(w);
        h = Math.round(h);

        st.w = w;
        st.h = h;
        st.dpr = window.devicePixelRatio || 1;
        canvas.style.width = w + 'px';
        canvas.style.height = h + 'px';
        canvas.width = Math.round(w * st.dpr);
        canvas.height = Math.round(h * st.dpr);
        st.ctx = canvas.getContext('2d');
        draw();
    }

    function setAspect(value) {
        st.aspect = value;

        var chips = document.querySelectorAll('#icRoot [data-ic-aspect]');
        for (var i = 0; i < chips.length; i++) {
            var v = parseFloat(chips[i].getAttribute('data-ic-aspect'));
            chips[i].classList.toggle('is-active', Math.abs(v - value) < 0.001);
        }

        resizeStage();
    }

    function setStatus(text) {
        var s = el('icStatus');
        if (!s) { return; }
        s.textContent = text || '';
        s.style.display = text ? 'flex' : 'none';
    }

    function bind() {
        var root = el('icRoot');
        var canvas = el('icCanvas');

        st.onResize = function () { resizeStage(); };
        window.addEventListener('resize', st.onResize);

        // Gdy Blazor zostawi te same elementy DOM (kolejne zdjęcie z kolejki),
        // nie podpinamy zdarzeń drugi raz.
        if (root._icBound) { return; }
        root._icBound = true;

        el('icZoom').addEventListener('input', function (e) {
            st.zoom = clamp(parseInt(e.target.value, 10) / 100, 1, 4);
            draw();
        });
        el('icPanX').addEventListener('input', function (e) {
            st.panX = clamp(parseInt(e.target.value, 10) / 100, -1, 1);
            draw();
        });
        el('icPanY').addEventListener('input', function (e) {
            st.panY = clamp(parseInt(e.target.value, 10) / 100, -1, 1);
            draw();
        });

        root.addEventListener('click', function (e) {
            var chip = e.target.closest ? e.target.closest('[data-ic-aspect]') : null;
            if (chip && st && st.ready) {
                setAspect(parseFloat(chip.getAttribute('data-ic-aspect')));
                return;
            }

            var center = e.target.closest ? e.target.closest('[data-ic-center]') : null;
            if (center && st && st.ready) {
                st.panX = 0; st.panY = 0;
                draw();
                return;
            }

            var reset = e.target.closest ? e.target.closest('[data-ic-reset]') : null;
            if (reset && st && st.ready) {
                st.zoom = 1; st.panX = 0; st.panY = 0;
                draw();
            }
        });

        // Przeciąganie zdjęcia myszą / palcem.
        var drag = null;

        canvas.addEventListener('pointerdown', function (e) {
            if (!st || !st.ready) { return; }
            drag = { x: e.clientX, y: e.clientY };
            canvas.setPointerCapture(e.pointerId);
            canvas.classList.add('is-dragging');
        });

        canvas.addEventListener('pointermove', function (e) {
            if (!drag || !st) { return; }
            var L = layout();
            var dx = e.clientX - drag.x;
            var dy = e.clientY - drag.y;
            drag.x = e.clientX;
            drag.y = e.clientY;

            if (L.maxX > 0) { st.panX = clamp(st.panX - dx / L.maxX, -1, 1); }
            if (L.maxY > 0) { st.panY = clamp(st.panY - dy / L.maxY, -1, 1); }
            draw();
        });

        function endDrag() {
            drag = null;
            canvas.classList.remove('is-dragging');
        }

        canvas.addEventListener('pointerup', endDrag);
        canvas.addEventListener('pointercancel', endDrag);

        // Kółko myszy = powiększenie.
        canvas.addEventListener('wheel', function (e) {
            if (!st || !st.ready) { return; }
            e.preventDefault();
            st.zoom = clamp(st.zoom + (e.deltaY < 0 ? 0.1 : -0.1), 1, 4);
            draw();
        }, { passive: false });
    }

    window.imageCropper.init = function (dataUrl) {
        window.imageCropper.dispose();

        if (!el('icRoot') || !el('icCanvas')) { return false; }

        st = {
            img: new Image(),
            zoom: 1, panX: 0, panY: 0,
            aspect: 0, w: 0, h: 0, dpr: 1,
            ready: false, ctx: null, onResize: null
        };

        bind();
        setStatus('Wczytywanie zdjęcia…');

        var current = st;

        current.img.onload = function () {
            if (st !== current) { return; }
            current.ready = true;
            setStatus('');
            setAspect(0);
        };

        current.img.onerror = function () {
            if (st !== current) { return; }
            setStatus('Nie udało się wyświetlić zdjęcia. Użyj opcji „Bez kadrowania”.');
        };

        current.img.src = dataUrl;
        return true;
    };

    function renderTo(maxW, maxH) {
        var r = sourceRect();
        var o = outputSize(maxW, maxH);
        var c = document.createElement('canvas');
        c.width = o.w;
        c.height = o.h;

        var ctx = c.getContext('2d');
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, o.w, o.h);
        ctx.imageSmoothingQuality = 'high';
        ctx.drawImage(st.img, r.sx, r.sy, r.sw, r.sh, 0, 0, o.w, o.h);
        return c;
    }

    // Zwraca strumień JPEG do odczytu po stronie .NET (IJSStreamReference).
    window.imageCropper.export = async function () {
        if (!st || !st.ready) { return null; }

        var canvas = renderTo(OUT_MAX, OUT_MAX);
        var blob = await new Promise(function (resolve) {
            canvas.toBlob(resolve, 'image/jpeg', 0.9);
        });

        if (!blob) { return null; }

        var buffer = await blob.arrayBuffer();
        return DotNet.createJSStreamReference(new Uint8Array(buffer));
    };

    window.imageCropper.thumbnail = function () {
        if (!st || !st.ready) { return null; }
        return renderTo(THUMB_W, THUMB_H).toDataURL('image/jpeg', 0.75);
    };

    window.imageCropper.dispose = function () {
        if (st && st.onResize) {
            window.removeEventListener('resize', st.onResize);
        }
        st = null;
    };
})();