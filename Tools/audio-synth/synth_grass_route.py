"""Procedural sound design for Grass Route (relaxing garden mower game).

Everything here is original synthesis -> owned by the studio, no licence needed.
Usage: python synth_grass_route.py [out_dir]   (needs numpy, scipy)
Output: 44.1 kHz stereo 16-bit WAV into out_dir (default ./out; do not commit it, copy the chosen files into Assets/GrassSimulation/Audio).
"""
import os
import sys
import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
RNG = np.random.default_rng(20261006)
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "out")
os.makedirs(OUT, exist_ok=True)


# ---------------------------------------------------------------- helpers
def t_axis(dur):
    return np.arange(int(dur * SR)) / SR


def db(x):
    return 10 ** (x / 20)


def bp(x, lo, hi, order=2):
    sos = signal.butter(order, [lo, hi], btype="band", fs=SR, output="sos")
    return signal.sosfilt(sos, x, axis=0)


def lp(x, f, order=2):
    sos = signal.butter(order, f, btype="low", fs=SR, output="sos")
    return signal.sosfilt(sos, x, axis=0)


def hp(x, f, order=2):
    sos = signal.butter(order, f, btype="high", fs=SR, output="sos")
    return signal.sosfilt(sos, x, axis=0)


def env_adsr(n, a, d, s, r, sus_level=0.6):
    a, d, r = int(a * SR), int(d * SR), int(r * SR)
    s = max(n - a - d - r, 0)
    e = np.concatenate([
        np.linspace(0, 1, a, endpoint=False) ** 1.5,
        np.linspace(1, sus_level, d, endpoint=False),
        np.full(s, sus_level),
        np.linspace(sus_level, 0, r) ** 2,
    ])
    return np.pad(e, (0, max(0, n - len(e))))[:n]


def env_perc(n, a, decay):
    t = np.arange(n) / SR
    att = np.clip(t / max(a, 1e-4), 0, 1)
    return att * np.exp(-t / decay)


def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def stereo(x, width=0.0, pan=0.0):
    """mono -> stereo with optional Haas widening and equal-power pan."""
    l = x.copy()
    r = x.copy()
    if width > 0:
        d = int(width * 0.012 * SR)
        r = np.concatenate([np.zeros(d), x[:-d]]) if d else r
    gl = np.cos((pan + 1) * np.pi / 4)
    gr = np.sin((pan + 1) * np.pi / 4)
    return np.stack([l * gl, r * gr], axis=1) * np.sqrt(2)


_IR_CACHE = {}


def reverb(x, decay=1.8, mix=0.25, predelay=0.012, bright=6000):
    """Convolution reverb with a synthetic decorrelated stereo IR."""
    key = (decay, predelay, bright)
    if key not in _IR_CACHE:
        n = int(decay * SR)
        t = np.arange(n) / SR
        ir = RNG.standard_normal((n, 2)) * np.exp(-6.9 * t / decay)[:, None]
        ir = lp(ir, bright)
        ir = np.concatenate([np.zeros((int(predelay * SR), 2)), ir])
        ir /= np.sqrt(np.sum(ir ** 2, axis=0))
        _IR_CACHE[key] = ir
    ir = _IR_CACHE[key]
    if x.ndim == 1:
        x = stereo(x)
    wet = np.stack([signal.fftconvolve(x[:, c], ir[:, c]) for c in range(2)], axis=1)
    out = np.zeros_like(wet)
    out[: len(x)] += x * (1 - mix)
    out += wet * mix
    return out


def soft_clip(x, drive=1.0):
    return np.tanh(x * drive) / np.tanh(drive)


def trim_tail(x, thresh_db=-60):
    a = np.max(np.abs(x), axis=1) if x.ndim == 2 else np.abs(x)
    idx = np.where(a > db(thresh_db) * a.max())[0]
    end = min(len(x), idx[-1] + int(0.02 * SR)) if len(idx) else len(x)
    x = x[:end]
    fade = min(int(0.02 * SR), len(x))
    x[-fade:] *= np.linspace(1, 0, fade)[:, None] if x.ndim == 2 else np.linspace(1, 0, fade)
    return x


def normalize(x, peak_db=-1.0):
    return x / (np.max(np.abs(x)) + 1e-12) * db(peak_db)


def save(name, x, peak_db=-1.0, trim=True):
    if x.ndim == 1:
        x = stereo(x)
    if trim:
        x = trim_tail(x)
    x = normalize(x, peak_db)
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (x * 32767).astype(np.int16))
    print(f"{name:32s} {len(x) / SR:6.2f}s")


def make_loop(x, xfade):
    """Seamless loop: crossfade the tail into the head (equal power)."""
    n = int(xfade * SR)
    head, body, tail = x[:n], x[n:-n] if n else x, x[-n:]
    w = np.linspace(0, np.pi / 2, n)
    if x.ndim == 2:
        w = w[:, None]
    mixed = tail * np.cos(w) + head * np.sin(w)
    return np.concatenate([body[: len(body)], mixed]) if n else x


# ---------------------------------------------------------------- instruments
def bell(f, dur=2.5, bright=1.0):
    """Soft FM bell / glass chime."""
    t = t_axis(dur)
    mod_env = np.exp(-t / (0.25 * bright))
    mod = np.sin(2 * np.pi * f * 3.5 * t) * 2.2 * mod_env * bright
    car = np.sin(2 * np.pi * f * t + mod)
    partial = 0.25 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t / 0.35)
    return (car + partial) * env_perc(len(t), 0.002, dur / 4.5)


def kalimba(f, dur=1.6, vel=1.0):
    """Kalimba: sine fundamental + inharmonic tine partials + soft click."""
    t = t_axis(dur)
    x = np.sin(2 * np.pi * f * t) * np.exp(-t / (0.55 * vel + 0.15))
    x += 0.35 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t / 0.06)
    x += 0.12 * np.sin(2 * np.pi * f * 3.0 * t) * np.exp(-t / 0.18)
    click = bp(RNG.standard_normal(len(t)), 1500, 5000) * np.exp(-t / 0.004) * 0.25
    att = np.clip(t / 0.0015, 0, 1)
    return (x + click) * att * vel


def marimba(f, dur=1.0, vel=1.0):
    t = t_axis(dur)
    x = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.35)
    x += 0.4 * np.sin(2 * np.pi * f * 4.0 * t) * np.exp(-t / 0.05)
    x += 0.15 * np.sin(2 * np.pi * f * 9.9 * t) * np.exp(-t / 0.015)
    return x * np.clip(t / 0.001, 0, 1) * vel


def epiano(f, dur=3.0, vel=0.8):
    """Warm Rhodes-like tine via 1:1 FM with decaying index + tremolo."""
    t = t_axis(dur)
    idx = 1.2 * vel * np.exp(-t / 0.6) + 0.15
    x = np.sin(2 * np.pi * f * t + idx * np.sin(2 * np.pi * f * t))
    x += 0.2 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t / 0.4)
    trem = 1 + 0.08 * np.sin(2 * np.pi * 4.5 * t)
    return x * env_adsr(len(t), 0.004, 1.2, 0, 0.6, 0.35) * trem * vel


def pad(freqs, dur):
    """Warm detuned saw pad, low-passed, slow attack/release."""
    t = t_axis(dur)
    x = np.zeros_like(t)
    for f in freqs:
        for det in (-0.12, 0.0, 0.11):
            ff = f * 2 ** (det / 12)
            ph = RNG.uniform(0, 1)
            x += signal.sawtooth(2 * np.pi * (ff * t + ph))
    x = lp(x, 1400, 2) * env_adsr(len(t), 1.2, 0.5, 0, 1.4, 0.9)
    return x / (len(freqs) * 3)


def add_at(buf, x, s):
    e = min(len(buf), s + len(x))
    buf[s:e] += x[: e - s]


def noise(n):
    return RNG.standard_normal(n)


# ---------------------------------------------------------------- gameplay SFX
def grass_cut_loop(name, density, dur=8.0, bright=1.0):
    """Continuous blade-through-grass texture. density = snips per second."""
    n = int(dur * SR)
    out = np.zeros((n, 2))
    # bed: airy blade whoosh
    bed = bp(noise(n), 700, 3500, 2)
    swirl = 0.75 + 0.25 * np.sin(2 * np.pi * 0.37 * t_axis(dur)) * np.sin(2 * np.pi * 0.11 * t_axis(dur))
    out += stereo(bed * swirl * 0.10, width=0.6)
    # snips: tiny bandpassed noise bursts = individual blades of grass
    k = int(density * dur)
    times = np.sort(RNG.uniform(0, dur - 0.08, k))
    gl = int(0.045 * SR)
    for tt in times:
        i = int(tt * SR)
        lo = RNG.uniform(1800, 3200)
        g = bp(noise(gl), lo, lo * RNG.uniform(1.8, 2.6), 2)
        g *= env_perc(gl, 0.0015, RNG.uniform(0.006, 0.016))
        g *= RNG.uniform(0.25, 1.0) ** 1.5
        # soft "tuk" body = stalk snapping
        body = lp(noise(gl), 600, 2) * env_perc(gl, 0.001, 0.008) * 0.5
        out[i:i + gl] += stereo(g * bright + body, pan=RNG.uniform(-0.5, 0.5))
    # gentle de-harsh: tame 6-10 kHz
    out = lp(out, 7500, 2)
    out = make_loop(out, 0.5)
    save(name, out, peak_db=-3, trim=False)


def mower_hum_loop(dur=8.0):
    """Quiet electric reel-mower motor: rounded, no buzz. Pitch can be scaled by speed in Unity."""
    t = t_axis(dur + 0.5)
    f0 = 92.5 * (1 + 0.004 * np.sin(2 * np.pi * 0.7 * t))
    ph = 2 * np.pi * np.cumsum(f0) / SR
    x = np.sin(ph) + 0.45 * np.sin(2 * ph) + 0.18 * np.sin(3 * ph) + 0.06 * np.sin(5 * ph)
    whir = bp(noise(len(t)), 300, 900) * 0.12 * (1 + 0.3 * np.sin(2 * np.pi * 12 * t))
    x = lp(x * 0.5 + whir, 900)
    x = stereo(x, width=0.3)
    x = make_loop(x, 0.5)
    save("sfx_mower_hum_loop", x, peak_db=-6, trim=False)


def grass_snip(i):
    """One-shot tuft cut (for big clumps / bushes)."""
    dur = 0.35
    n = int(dur * SR)
    x = np.zeros(n)
    count = RNG.integers(5, 9)
    for _ in range(count):
        s = int(RNG.uniform(0, 0.09) * SR)
        gl = int(0.06 * SR)
        lo = RNG.uniform(1600, 3000)
        g = bp(noise(gl), lo, lo * 2.3) * env_perc(gl, 0.001, RNG.uniform(0.008, 0.02))
        x[s:s + gl] += g * RNG.uniform(0.4, 1)
    swish = bp(noise(n), 900, 4000) * env_perc(n, 0.02, 0.07) * 0.5
    thump = lp(noise(n), 250) * env_perc(n, 0.002, 0.03) * 1.2
    x = lp(x + swish + thump, 8000)
    save(f"sfx_grass_snip_{i:02d}", reverb(x, 0.6, 0.12), peak_db=-4)


def bush_rustle(i):
    """Leafy bush being trimmed: longer, softer crunch."""
    dur = 0.7
    n = int(dur * SR)
    x = np.zeros(n)
    for _ in range(RNG.integers(14, 22)):
        s = int(RNG.uniform(0, 0.35) * SR)
        gl = int(0.08 * SR)
        lo = RNG.uniform(900, 2200)
        x[s:s + gl] += bp(noise(gl), lo, lo * 3) * env_perc(gl, 0.003, RNG.uniform(0.01, 0.04)) * RNG.uniform(0.3, 1)
    x = lp(x, 6500) * env_adsr(n, 0.01, 0.3, 0, 0.3, 0.5)
    save(f"sfx_bush_trim_{i:02d}", reverb(x, 0.8, 0.15), peak_db=-4)


def fruit_pop(i):
    """Juicy, cute fruit slice: wet squelch + bubbly pitch-up pop."""
    dur = 0.5
    t = t_axis(dur)
    n = len(t)
    f = 420 * RNG.uniform(0.9, 1.15)
    sweep = f * (1 + 1.6 * (1 - np.exp(-t / 0.03)))
    pop = np.sin(2 * np.pi * np.cumsum(sweep) / SR) * env_perc(n, 0.001, 0.05)
    squelch = bp(noise(n), 400, 2200) * env_perc(n, 0.002, 0.06) * 0.6
    slice_ = bp(noise(n), 2500, 6000) * env_perc(n, 0.0005, 0.01) * 0.3
    drip = sum(
        np.sin(2 * np.pi * midi(RNG.integers(76, 86)) * t) * env_perc(n, 0.001, 0.03) *
        (t > d) * 0.15 for d in RNG.uniform(0.06, 0.2, 2))
    save(f"sfx_fruit_pop_{i:02d}", reverb(pop + squelch + slice_ + drip, 0.9, 0.18), peak_db=-3)


def quota_complete():
    """Warm 'ding-ding' two-note glass chime (major 3rd + 5th shimmer)."""
    dur = 2.6
    x = np.zeros(int(dur * SR))
    for k, (m, d, v) in enumerate([(84, 0.0, 0.9), (88, 0.09, 1.0), (91, 0.18, 0.55), (96, 0.18, 0.25)]):
        b = bell(midi(m), dur - d, bright=0.7) * v
        s = int(d * SR)
        add_at(x, b, s)
    save("sfx_quota_complete", reverb(x, 2.2, 0.32), peak_db=-2)


def tier_up():
    """Rising kalimba arpeggio + sparkle tail = mower grew bigger."""
    notes = [72, 76, 79, 84, 88, 91]
    dur = 2.8
    x = np.zeros(int(dur * SR))
    for k, m in enumerate(notes):
        s = int(k * 0.065 * SR)
        k1 = kalimba(midi(m), dur - k * 0.065, vel=0.6 + 0.08 * k)
        add_at(x, k1, s)
    t = t_axis(dur)
    sparkle = np.zeros_like(t)
    for _ in range(18):
        st = RNG.uniform(0.3, 1.3)
        f = midi(RNG.choice([96, 100, 103, 108]))
        ts = np.clip(t - st, 0, None)
        sparkle += np.sin(2 * np.pi * f * ts) * np.exp(-ts / 0.08) * (t >= st) * 0.06
    whoosh = bp(noise(len(t)), 2000, 9000) * env_adsr(len(t), 0.35, 0.3, 0, 0.6, 0.3) * 0.08
    up = np.sin(2 * np.pi * np.cumsum(midi(60) * (1 + 1.0 * np.clip(t / 0.45, 0, 1))) / SR) * env_adsr(len(t), 0.02, 0.4, 0, 0.1, 0) * 0.25
    save("sfx_tier_up", reverb(x + whoosh + up + sparkle, 2.0, 0.3), peak_db=-2)


def upgrade_chosen():
    """Soft 'pop' + two-tone confirm."""
    dur = 1.2
    t = t_axis(dur)
    n = len(t)
    pop = np.sin(2 * np.pi * np.cumsum(300 + 900 * (1 - np.exp(-t / 0.02))) / SR) * env_perc(n, 0.001, 0.04)
    a = marimba(midi(79), dur, 0.7)
    b = np.roll(marimba(midi(86), dur, 0.8), int(0.08 * SR))
    b[: int(0.08 * SR)] = 0
    save("sfx_upgrade_chosen", reverb(pop * 0.6 + a + b, 1.4, 0.25), peak_db=-2)


def protected_hit():
    """Gentle 'oops': muffled wooden bonk + soft descending minor 2nd. Not harsh."""
    dur = 0.9
    t = t_axis(dur)
    n = len(t)
    bonk = np.sin(2 * np.pi * np.cumsum(220 * np.exp(-t / 0.08) + 110) / SR) * env_perc(n, 0.001, 0.07)
    tone_a = marimba(midi(67), dur, 0.6)
    tone_b = np.roll(marimba(midi(66), dur, 0.55), int(0.11 * SR))
    tone_b[: int(0.11 * SR)] = 0
    x = lp(bonk * 0.9 + tone_a + tone_b, 3000)
    save("sfx_protected_hit", reverb(x, 0.9, 0.15), peak_db=-3)


def timer_tick(i, accent=False):
    dur = 0.25
    t = t_axis(dur)
    f = 1250 if accent else 950
    x = np.sin(2 * np.pi * f * t) * env_perc(len(t), 0.0005, 0.025)
    x += bp(noise(len(t)), 2000, 6000) * env_perc(len(t), 0.0003, 0.004) * 0.3
    save(f"sfx_timer_tick{'_accent' if accent else ''}", reverb(x, 0.4, 0.1), peak_db=-6)


def coin(i):
    dur = 0.9
    a = lp(bell(midi(83 + i), dur, 0.4), 5000) * 0.7
    b = np.roll(lp(bell(midi(88 + i), dur, 0.4), 5000), int(0.06 * SR))
    b[: int(0.06 * SR)] = 0
    save(f"sfx_coin_{i:02d}", reverb(a + b, 1.0, 0.2), peak_db=-4)


def jingle_win():
    """Short cheerful kalimba + epiano phrase (C major), ~3.5 s."""
    bpm = 132
    beat = 60 / bpm
    phrase = [(72, 0, .5), (76, .5, .5), (79, 1, .5), (84, 1.5, 1.0), (83, 2.5, .5), (84, 3.0, 2.5)]
    dur = 5.5
    x = np.zeros(int(dur * SR))
    for m, st, ln in phrase:
        s = int(st * beat * SR)
        k = kalimba(midi(m), min(2.2, dur - st * beat), 0.9)
        add_at(x, k, s)
    for chord, st in (([60, 64, 67], 0), ([65, 69, 72], 1.5), ([60, 64, 67, 71], 3.0)):
        s = int(st * beat * SR)
        for m in chord:
            e = epiano(midi(m), dur - st * beat, 0.5) * 0.35
            add_at(x, e, s)
    save("jingle_level_win", reverb(x, 2.4, 0.3), peak_db=-1.5)


def jingle_lose():
    """Kind, non-punishing 'try again' phrase: descending, ends on a hopeful IV."""
    beat = 60 / 100
    phrase = [(76, 0, .5), (74, .5, .5), (72, 1, .5), (69, 1.5, 1.5)]
    dur = 4.5
    x = np.zeros(int(dur * SR))
    for m, st, ln in phrase:
        s = int(st * beat * SR)
        k = kalimba(midi(m), min(2.2, dur - st * beat), 0.8)
        add_at(x, k, s)
    for chord, st in (([57, 60, 64], 0), ([53, 57, 60, 64], 1.5)):
        s = int(st * beat * SR)
        for m in chord:
            e = epiano(midi(m), dur - st * beat, 0.45) * 0.35
            add_at(x, e, s)
    save("jingle_level_lose", reverb(x, 2.4, 0.3), peak_db=-2)


def level_start():
    """Soft 'whoosh-pling' when the round begins."""
    dur = 1.6
    t = t_axis(dur)
    whoosh = bp(noise(len(t)), 500, 4000) * env_adsr(len(t), 0.25, 0.2, 0, 0.3, 0.2) * 0.4
    k = np.zeros_like(t)
    for j, m in enumerate([79, 84]):
        s = int((0.22 + j * 0.08) * SR)
        kk = kalimba(midi(m), dur - 0.4, 0.8)
        add_at(k, kk, s)
    save("sfx_level_start", reverb(whoosh + k, 1.6, 0.28), peak_db=-2)


# ---------------------------------------------------------------- UI
def ui_tap(i):
    """Rounded bubble pop for buttons."""
    dur = 0.18
    t = t_axis(dur)
    f0 = [520, 600, 680][i]
    sweep = f0 * (1 + 0.8 * (1 - np.exp(-t / 0.012)))
    x = np.sin(2 * np.pi * np.cumsum(sweep) / SR) * env_perc(len(t), 0.0008, 0.03)
    save(f"ui_tap_{i:02d}", reverb(x, 0.3, 0.08), peak_db=-6)


def ui_popup(open_=True):
    dur = 0.45
    t = t_axis(dur)
    a, b = (midi(79), midi(86)) if open_ else (midi(86), midi(79))
    x = marimba(a, dur, 0.6)
    y = np.roll(marimba(b, dur, 0.6), int(0.05 * SR))
    y[: int(0.05 * SR)] = 0
    air = bp(noise(len(t)), 1500, 6000) * env_adsr(len(t), 0.06 if open_ else 0.01, 0.1, 0, 0.1, 0.3) * 0.15
    save(f"ui_popup_{'open' if open_ else 'close'}", reverb(x + y + air, 0.8, 0.18), peak_db=-6)


def ui_toggle(on=True):
    dur = 0.15
    t = t_axis(dur)
    f = 900 if on else 700
    x = np.sin(2 * np.pi * f * t) * env_perc(len(t), 0.0005, 0.018)
    x += bp(noise(len(t)), 3000, 7000) * env_perc(len(t), 0.0002, 0.003) * 0.4
    save(f"ui_toggle_{'on' if on else 'off'}", x, peak_db=-8)


def ui_star(i):
    """Result-screen star reveal, rising per star."""
    m = [84, 88, 91][i]
    dur = 1.4
    x = bell(midi(m), dur, 0.8) + 0.4 * kalimba(midi(m - 12), dur, 0.6)
    save(f"ui_star_{i + 1}", reverb(x, 1.6, 0.3), peak_db=-3)


# ---------------------------------------------------------------- music
def music_garden_loop():
    """'Morning Lawn' - 80 BPM lo-fi kalimba loop, 16 bars, seamless.

    Chords: Fmaj7 - Em7 - Dm7 - Cmaj7(add9)  x4, melody varies per pass.
    """
    bpm = 80
    beat = 60 / bpm
    bars = 16
    total = bars * 4 * beat
    tail = 3.0
    n = int((total + tail) * SR)
    mix = np.zeros((n, 2))

    chords = [[53, 57, 60, 64], [52, 55, 59, 62], [50, 53, 57, 60], [48, 52, 55, 59, 62]]
    bass = [41, 40, 38, 36]
    scale = [60, 62, 64, 67, 69, 72, 74, 76, 79]  # C major pentatonic-ish

    def put(buf, x, start):
        s = int(start * SR)
        e = min(len(buf), s + len(x))
        buf[s:e] += x[: e - s]

    # pad + epiano comp
    pad_buf = np.zeros(n)
    ep_buf = np.zeros(n)
    bass_buf = np.zeros(n)
    for bar in range(bars):
        ch = chords[bar % 4]
        st = bar * 4 * beat
        put(pad_buf, pad([midi(m) for m in ch], 4 * beat + 1.5), st)
        for hit, vel in ((0, 0.55), (1.5, 0.35), (2.5, 0.4)):
            for m in ch:
                put(ep_buf, epiano(midi(m), 2.5, vel) * 0.25, st + hit * beat + RNG.uniform(0, 0.015))
        # round sub bass, soft
        for hit in (0, 2.5):
            tt = t_axis(1.6)
            b = np.sin(2 * np.pi * midi(bass[bar % 4]) * tt) * env_adsr(len(tt), 0.01, 0.4, 0, 0.5, 0.6)
            put(bass_buf, b * 0.5, st + hit * beat)

    # kalimba melody: motif-based, 8th notes with rests, humanised
    mel_buf = np.zeros(n)
    motif_rng = np.random.default_rng(7)
    motifs = []
    for _ in range(4):
        steps = []
        idx = motif_rng.integers(2, 6)
        for k in range(8):
            if motif_rng.random() < 0.35 and k not in (0,):
                steps.append(None)
                continue
            idx = int(np.clip(idx + motif_rng.choice([-2, -1, -1, 0, 1, 1, 2]), 0, len(scale) - 1))
            steps.append(scale[idx])
        motifs.append(steps)
    for bar in range(bars):
        if bar < 2:  # intro: harmony only, melody enters on bar 3
            continue
        motif = motifs[(bar // 2) % 4] if bar % 2 == 0 else motifs[(bar // 2 + 1) % 4][::-1]
        for k, m in enumerate(motif):
            if m is None:
                continue
            swing = 0.06 * beat if k % 2 else 0
            st = bar * 4 * beat + k * 0.5 * beat + swing + RNG.uniform(-0.008, 0.008)
            put(mel_buf, kalimba(midi(m), 1.8, RNG.uniform(0.55, 0.85)), st)
            if RNG.random() < 0.18:  # occasional octave sparkle
                put(mel_buf, kalimba(midi(m + 12), 1.2, 0.25), st + 0.5 * beat)

    # soft brushed shaker + rim, lo-fi texture
    perc_buf = np.zeros(n)
    for bar in range(4, bars):
        for k in range(8):
            st = bar * 4 * beat + k * 0.5 * beat + (0.05 * beat if k % 2 else 0)
            gl = int(0.09 * SR)
            sh = bp(noise(gl), 4000, 10000) * env_perc(gl, 0.006, 0.03) * (0.10 if k % 2 else 0.16)
            put(perc_buf, sh, st)
        for k in (1, 3):
            gl = int(0.15 * SR)
            rim = (bp(noise(gl), 600, 2500) * 0.6 + np.sin(2 * np.pi * 330 * t_axis(0.15))) * env_perc(gl, 0.001, 0.02) * 0.18
            put(perc_buf, lp(rim, 3000), bar * 4 * beat + k * beat)
        gl = int(0.3 * SR)
        tt = t_axis(0.3)
        kick = np.sin(2 * np.pi * np.cumsum(50 + 70 * np.exp(-tt / 0.03)) / SR) * env_perc(gl, 0.002, 0.12) * 0.55
        put(perc_buf, kick, bar * 4 * beat)
        put(perc_buf, kick * 0.6, bar * 4 * beat + 2.5 * beat)

    # vinyl crackle / tape air
    vin = bp(noise(n), 1000, 6000) * 0.006
    crack = (RNG.random(n) < 0.00025) * RNG.standard_normal(n) * 0.15
    vin += hp(crack, 2000)

    mix += stereo(pad_buf * 0.55, width=0.8)
    mix += stereo(ep_buf, width=0.3, pan=-0.15)
    mix += stereo(bass_buf * 0.9)
    mix += reverb(stereo(mel_buf * 0.8, width=0.2, pan=0.12), 2.5, 0.32)[:n]
    mix += stereo(perc_buf, width=0.4)
    mix = reverb(mix, 1.6, 0.14)[:n]
    mix += stereo(vin, width=1.0)

    # tape warmth: gentle low-pass + saturation
    mix = lp(mix, 9000)
    mix = soft_clip(mix / np.max(np.abs(mix)) * 1.1, 1.4)

    # wrap reverb tail onto the head for a seamless loop
    L = int(total * SR)
    loop = mix[:L].copy()
    tail_part = mix[L:]
    loop[: len(tail_part)] += tail_part
    save("music_morning_lawn_loop", loop, peak_db=-1.5, trim=False)


def ambience_wind_loop():
    """Soft breeze + leaf rustle bed, 30 s seamless. Layer with bird ambience."""
    dur = 30
    n = int(dur * SR)
    t = t_axis(dur)
    gust = 0.6 + 0.4 * (0.5 + 0.5 * np.sin(2 * np.pi * t / 11.0)) * (0.5 + 0.5 * np.sin(2 * np.pi * t / 4.3 + 1))
    wind = lp(noise(n), 500) * gust
    leaves = bp(noise(n), 2500, 7000) * (gust ** 3) * 0.25
    x = np.stack([wind + leaves, lp(noise(n), 500) * gust + bp(noise(n), 2500, 7000) * gust ** 3 * 0.25], axis=1)
    x = make_loop(x, 2.0)
    save("amb_garden_breeze_loop", x, peak_db=-6, trim=False)


if __name__ == "__main__":
    grass_cut_loop("sfx_cut_loop_light", density=14, bright=0.9)
    grass_cut_loop("sfx_cut_loop_medium", density=32)
    grass_cut_loop("sfx_cut_loop_dense", density=70, bright=1.1)
    mower_hum_loop()
    for i in range(4):
        grass_snip(i)
    for i in range(3):
        bush_rustle(i)
    for i in range(3):
        fruit_pop(i)
    for i in range(3):
        coin(i)
    quota_complete()
    tier_up()
    upgrade_chosen()
    protected_hit()
    timer_tick(0)
    timer_tick(0, accent=True)
    level_start()
    jingle_win()
    jingle_lose()
    for i in range(3):
        ui_tap(i)
    ui_popup(True)
    ui_popup(False)
    ui_toggle(True)
    ui_toggle(False)
    for i in range(3):
        ui_star(i)
    ambience_wind_loop()
    music_garden_loop()
