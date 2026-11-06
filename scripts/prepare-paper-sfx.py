"""Prepare the selected CC0 Kenney recordings; keep complete takes and the source ZIP."""
import hashlib
import json
import subprocess
import wave
import zipfile
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT / '.local/kenney-rpg-audio-original.zip'
OUTPUT = ROOT / 'public/media/sfx'
OUTPUT.mkdir(parents=True, exist_ok=True)
TAKES = [('bookFlip1', 'paper-turn-a', .060), ('bookFlip2', 'paper-turn-b', .060),
         ('bookFlip3', 'paper-slip', .052), ('bookPlace1', 'paper-press', .075)]
report = {'source': 'https://kenney.nl/assets/rpg-audio', 'author': 'Kenney Vleugels',
          'license': 'CC0 1.0', 'archive': str(ARCHIVE.relative_to(ROOT)),
          'archiveSha256': hashlib.sha256(ARCHIVE.read_bytes()).hexdigest(),
          'processing': 'Complete takes, mono 48 kHz; high-pass 120 Hz, low-pass 6800 Hz; '
                        'RMS adjustment, tanh soft limiter at 0.55; 2 ms in / 8 ms out fades. No synthetic layers.',
          'humanListeningApproved': False, 'files': []}
with zipfile.ZipFile(ARCHIVE) as archive:
    (OUTPUT / 'LICENSE-Kenney.txt').write_bytes(archive.read('License.txt'))
    for original, name, target_rms in TAKES:
        source = archive.read('Audio/' + original + '.ogg')
        result = subprocess.run(['ffmpeg', '-v', 'error', '-i', 'pipe:0', '-af',
            'highpass=f=120,lowpass=f=6800', '-ar', '48000', '-ac', '1', '-f', 'f32le', 'pipe:1'],
            input=source, capture_output=True, check=True)
        samples = np.frombuffer(result.stdout, dtype='<f4').copy()
        samples *= target_rms / max(float(np.sqrt(np.mean(samples * samples))), .00001)
        samples = .55 * np.tanh(samples / .55)
        samples[:96] *= np.linspace(0, 1, 96)
        samples[-384:] *= np.linspace(1, 0, 384)
        destination = OUTPUT / (name + '.wav')
        with wave.open(str(destination), 'wb') as wav:
            wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(48000)
            wav.writeframes(np.round(samples * 32767).astype('<i2').tobytes())
        report['files'].append({'file': str(destination.relative_to(ROOT)), 'original': 'Audio/' + original + '.ogg',
            'sourceSha256': hashlib.sha256(source).hexdigest(), 'sha256': hashlib.sha256(destination.read_bytes()).hexdigest(),
            'duration': len(samples) / 48000, 'bytes': destination.stat().st_size,
            'peak': float(abs(samples).max()), 'rms': float(np.sqrt(np.mean(samples * samples)))})
(ROOT / 'docs/paper-sfx-source.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
