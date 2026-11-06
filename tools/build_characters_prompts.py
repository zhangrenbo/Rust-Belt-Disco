# -*- coding: utf-8 -*-
"""Build the 37-character portrait batch record (38 jobs) from 背景设定.ink.
Style: same user-selected direction as the 40-scene snow batch (3D photorealistic,
UE5 quality) adapted to character portraits -- waist-up, 2:3, no orthographic camera.
Appearance comes from each character's 外貌/美术参考 section; 6 characters lack one
and their prompts are derived from story roles (marked 'derived': True).
Run: python -X utf8 tools/build_characters_prompts.py
"""
import hashlib, json, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INK = ROOT / 'content' / 'story' / 'ink' / '背景设定.ink'
OUT = ROOT / '美术概念' / 'MJ人物37个_v1_20260917.json'

PREFIX = ("Atmospheric photorealistic 3D character portrait, Unreal Engine 5 visual quality, "
          "realistic skin with pores, physically based surface shading, cinematic soft lighting, "
          "waist-up shot of ")
TAIL = (". 1990s Northeast China small-town setting, muted realistic colors, quiet cold immersive "
        "atmosphere. --ar 2:3 --raw --v 8.1 --relax --no wide-angle lens, perspective distortion, "
        "painting, illustration, brushstrokes, cartoon, miniature, interface, text, watermark, "
        "extra people, deformed hands, extra fingers")

# (id, name, block, age, prompt_body, derived_from_story)
CHARACTERS = [
    ('ND-C001', '董点事', 'BG_0013', '18',
     "a Chinese young man of 18, 175 cm tall, thin undeveloped adolescent build, narrow shoulders and prominent collarbones, short messy natural black hair with one lock falling over his forehead, pale unhealthy skin, faint dark circles under his eyes, a tired gaze beyond his age, delicate refined features, wearing an old T-shirt, jeans and canvas shoes with a worn canvas backpack, a thin scar on the web of his right hand. Quiet but unyielding, like he is pressing something down", False),
    ('ND-C002', '李正', 'BG_0014', '35',
     "a Chinese plainclothes police detective of 35, 178 cm tall, medium-thin but broad-shouldered build, straight upright police posture, standard crew cut with a few gray hairs at the temples, dark tanned skin, deep nasolabial folds and a permanent vertical frown line, sharp but weary eyes, a faint old scar below his left eye, cigarette stain on his right index finger, wearing a dark jacket with a police radio on his belt. Tired yet vigilant, like a compressed spring", False),
    ('ND-C003', '王少鹏', 'BG_0015', '35',
     "a Chinese traditional-medicine practitioner of 35, 176 cm tall, balanced slender build, upright refined bearing, neatly styled side-parted black hair with pomade, clean fair skin, handsome regular features with an approachable smile that reaches his eyes, wearing a clean white lab coat with a fountain pen in the breast pocket over a polo shirt and slacks, a possibly-fake expensive watch on his wrist. Cultured and composed, with a subtle tension beneath the polished surface", False),
    ('ND-C004', '王宇鹏', 'BG_0016', '35',
     "a Chinese man of 35, a twin brother with the same face as Wang Shaopeng but a radically different presence: 176 cm, slightly bulkier frame hardened by thirteen years of rough street survival, darker rougher sun-worn skin, unkempt shaggy black hair, an old oversized T-shirt, track pants and canvas shoes, a worn canvas backpack holding everything he owns, burn scars on his hands, untrimmed nails. His eyes are the key: empty, mirror-like, reflecting everything and absorbing nothing, looking at people as if at objects. A polished shadow, silent like a cat", False),
    ('ND-C005', '刘国庆', 'BG_0017', '50',
     "a Chinese auto-repair shop owner of 50, 172 cm tall, stocky solid build, flat-top crew cut with one third of his hair gone white, dark yellowish rough skin with deep wrinkles, big calloused hands with engine oil permanently in the nail creases, an old scar on the web of his left hand, wearing blue work overalls, a white undershirt and black cloth shoes with a key ring on his belt. Silent but steady, an old iron block of a man", False),
    ('ND-C006', '陈国涛', 'BG_0020', '52',
     "a Chinese county public-security bureau chief of 52, 175 cm tall, portly with a beer belly yet still imposing, ramrod-straight back, hair slicked back with pomade dyed black with white roots showing, square face with downturned mouth corners, wearing a short-sleeved white shirt and dark trousers with a key ring on his belt and a thick gold ring on his finger. Heavy wall-like authority; his smile never reaches his eyes", False),
    ('ND-C007', '董点事的母亲', 'BG_0002', '33',
     "a Chinese small-shop owner woman in her early 30s, 163 cm tall, small neat efficient build, short hair cut neatly to her ears, pale skin, hands roughened by shelf-stacking and ledger-writing, long worry lines on her face, wearing a neatly folded apron over a plain cotton blouse with a pen tucked in the apron pocket, holding a worn ledger book. A capable spirit of a laid-off worker turned shopkeeper, remembered in memory", False),
    ('ND-C008', '刘建国·壮年', 'BG_0003', '30',
     "a Chinese steel-plant workshop director in his 30s, 178 cm tall, broad shoulders and thick back, square face, thick eyebrows and a stubbled jaw, wearing a factory work uniform, standing at the steel plant gate with a rare genuine smile. A family-album photo reference", False),
    ('ND-C008B', '刘建国·晚年', 'BG_0003', '50',
     "a Chinese laid-off worker of 50, once broad-shouldered now visibly thinner and hunched, sunken eyes, a chronically alcoholic flushed face with yellowish whites of the eyes, yellow cigarette stains on his fingertips, wearing an old padded work jacket and a white undershirt, embroidered cloth shoes. A worn-out silhouette at the end of a long slide", False),
    ('ND-C009', '张秀梅', 'BG_0018', '50',
     "a Chinese woman of 50, 160 cm tall, slightly plump but solid from manual labor, back slightly bent, short hair to her ears held with a black hairpin, gray-streaked undyed hair, rough dark skin, slightly deformed knuckles, wearing a flowery cotton blouse, dark trousers and a long-worn apron with black cloth shoes. Like a well-used iron wok — unremarkable but utterly reliable", False),
    ('ND-C010', '高飞', 'BG_0010', '22',
     "a Chinese clinic clerk of 22, 174 cm tall, thin but wiry from physical work, narrow shoulders and slim waist, unkempt short black hair with a few pimples on his forehead, yellowish skin with lingering baby fat making him look younger, wearing a spotlessly clean white lab coat over cheap sports clothes, his hands conspicuously clean, holding a smartphone. Honest and quiet, with an occasional troubled crease between his brows", False),
    ('ND-C011', '王涛', 'BG_0019', '30',
     "a Chinese criminal police officer of 30, 180 cm tall, slender upright build, neat styled short hair, fair clean skin with a young face, wearing a crisply ironed shirt and suit trousers with a good wristwatch, quick restless darting eyes. Slick, fast and sharp like a loach", False),
    ('ND-C012', '小燕', 'BG_0021', '26',
     "a Chinese newlywed bride of 26, 163 cm tall, slender figure, round face with a small pointed chin, long naturally black glossy hair, fair clear skin, a dimpled smile, wearing a red qipao wedding dress with a red flower in her hair. Gentle and quiet like morning dew in a small town", False),
    ('ND-C013', '刘小红', 'BG_0022', '26',
     "a Chinese woman of 26 who looks five years older, 160 cm tall, thin from prison hardship with prominent collarbones and thin wrists, half-length dull yellowish hair hanging loose, sallow dark skin, sunken eyes, very short nails, wearing ill-fitting second-hand clothes. Like paper soaked by rain — still holding its shape but crumbling at a touch; resentment and unwillingness in her eyes", False),
    ('ND-C014', '韩铁军', 'BG_0011', '48',
     "a Chinese auto-repair shop worker of 48, 175 cm tall, stout with broad shoulders and thick arms, buzz cut with a receding hairline, dark yellow skin with grease smudges that never quite wash off, calloused scarred hands with black oil permanently in the nail creases, wearing blue work pants, a grease-stained undershirt and work boots, grinning wide with yellowed teeth. Warm and loud, a charcoal brazier of a man", False),
    ('ND-C015', '姜振山', 'BG_0023', '62',
     "a Chinese old traditional-medicine master of 62, 170 cm tall, lean and wiry with bright energetic eyes, his most iconic feature a permed head of small gray-white curls, thick eyebrows mixed with white, yellowed cigarette-stained fingers, wearing a Chinese mandarin-collar shirt or an old leather jacket with cloth shoes and a semi-fake prayer bead necklace, a cigarette in his mouth. A retired jianghu hero with a slow voice and every word carrying weight", False),
    ('ND-C016', '刘虎', 'BG_0024', '12',
     "a Chinese boy of 12, 140 cm tall, small and thin with narrow shoulders and thin wrists, a student short haircut with a wide forehead, pale skin with light freckles, big eyes usually lowered, dry chapped lips, wearing a school uniform of a white shirt and blue pants, a backpack missing a button, always holding a pen. Quieter than other kids, like a snail ready to retreat into its shell", False),
    ('ND-C017', '孙福来', 'BG_0026', '66',
     "a Chinese vocational-school gate guard of 66, 175 cm tall, thin and tall with a slightly hunched back, a long face with high cheekbones, sparse gray-white hair, wearing a faded dark-blue old security uniform, sitting in the guard booth sipping tea over a newspaper with a chess board on the table", False),
    ('ND-C018', '姜桂兰', 'BG_0025', '48',
     "a Chinese noodle-shop owner woman of 48, 160 cm tall, plump like a good cook with strong arms, short hair pinned under a hairnet, a ruddy glowing complexion, wearing a white oil-stained apron over a cotton shirt, rough reddened hands, a loud voice and a smiling face, the warmth of a busy kitchen around her", False),
    ('ND-C019', '杨秀文', 'BG_0012', '53',
     "a Chinese bookstore owner woman of 53, 162 cm tall, medium-thin with a remarkably straight back, hair in a ponytail with undyed white strands, reading glasses pushed up on her forehead, fair indoor-worker skin, calluses from decades of holding a pen, wearing a plain shirt with sleeve covers and a washed-out blue cloth apron. A quiet intellectual air that feels out of place in a small Northeast town", False),
    ('ND-C020', '马文华', 'BG_0031', '45',
     "a Chinese businessman and real-estate project manager of 45, 176 cm tall, plump from socializing, high hairline with pomaded slicked-back hair, wearing a tightly buttoned shirt and suit trousers with a thick gold chain on his neck, a grand confident business-dealing expression", False),
    ('ND-C021', '张德顺', 'BG_0029', '40',
     "a Chinese police officer of 40, 173 cm tall, medium build, the keyword is ordinary — a slightly untidy police uniform with a button sometimes left undone, small but sharp eyes that quietly record everything, a natural lack of presence: you would not remember him, but he remembers you", False),
    ('ND-C022', '周建设', 'BG_0030', '55',
     "a Chinese forensic technician of 55, 170 cm tall, thin with a back slightly hunched from years over a microscope, thick-lensed glasses, remarkably steady hands, wearing a white lab coat or an old jacket with two pens in his pocket. Quiet and cautious, like evidence sealed in a steel cabinet", False),
    ('ND-C023', '佟秀云', 'BG_0027', '80',
     "a Chinese elderly woman of 80, shrunk to 150 cm, extremely thin and hunched like a dried old tree root, all-white hair in a bun fixed with a black hairnet, a face of wrinkles like a dried riverbed, wearing a dark old cloth shirt, leaning on a black wooden cane — her iconic prop. Cloudy eyes with a piercing clarity that has seen too many people; a hoarse but clear voice", False),
    ('ND-C024', '周桂芝', 'BG_0028', '70',
     "a Chinese elderly woman of 70, 155 cm tall, thin and dried-out, hands deformed by years of collecting scrap with knobby bent finger joints, gray-white hair loosely tied, wearing an old washed-out cotton shirt and cloth shoes, stubborn unyielding eyes — the eyes of a woman who has repeated one sentence for thirteen years", False),
    ('ND-C025', '刘长河', 'BG_0033', '75',
     "a Chinese retired police officer of 75, once 180 cm tall now slightly shrunken, old but still tall, back beginning to hunch, slightly trembling hands, wearing an old Mao suit unchanged for decades, holding a radio. An old system man who always chose discipline over conscience", False),
    ('ND-C026', '徐志刚', 'BG_0032', '40',
     "a Chinese middle-school teacher of 40, 172 cm tall, medium build, terrifyingly ordinary — the kind of face that vanishes into a crowd — wearing a white shirt always tucked into his trousers and carrying a classic document bag, with a permanent polite smile that chills the blood", False),
    ('ND-C027', '丫丫·陈秀枝', 'BG_0034', '18(心智5)',
     "a Chinese girl of 18 whose body and mind stopped at about five years old, only 130 cm tall, extremely thin with wrists like twigs, messy fine hair tied on both sides with cheap plastic hair ties, a round face with big clear eyes of unsettling transparency, wearing an old floral cotton dress and sandals, crouching by a wall drawing circles on the ground. Not a child's innocence — the stillness of something frozen in time", False),
    ('ND-C028', '朱启渲', 'BG_0035', '25',
     "a Chinese woman of 25 lying unconscious in a hospital ICU bed, 60 percent of her body burned, her head and face wrapped in gauze bandages, an oxygen tube and IV lines, a breathing machine beside the bed, dim cold hospital light. A body that still reacts to a name", True),
    ('ND-C029', '赵淑兰', 'BG_0036', '35',
     "a Chinese housewife of 35, medium height, shoulder-length hair, plain neat clothes, a gentle tired face with faint sadness and the quiet ache of being overlooked, standing in a modest home interior. An ordinary wife long neglected", True),
    ('ND-C030', '李长胜', 'BG_0037', '58',
     "a Chinese town party secretary of 58, medium build, gray hair combed back, a composed stern face with deep-set eyes, wearing a dark cadre jacket over a shirt, the bearing of the actual highest authority in town — heavy, calm, untouchable, never seen directly", True),
    ('ND-C031', '王淑英', 'BG_0038', '75',
     "a Chinese elderly woman of 75 lying in a hospital bed in a vegetative state, thin and frail, all-white hair spread on the pillow, a breathing tube, a blanket pulled to her chest, winter light from the window behind. The grandmother who raised two abandoned grandsons alone", True),
    ('ND-C033', '孙翠花', 'BG_0040', '42',
     "a Chinese scrap-collecting mother of 42, thin and weather-beaten, gray-streaked hair tied back loosely, patched old cotton clothes and cloth shoes, rough worn hands from years of collecting waste, a tired face that has long gone numb to the town calling her daughter an idiot — but a faint crease of pain appears when they do. Thirteen years of quiet exhaustion", True),
    ('ND-C034', '马骏', 'BG_0043', '35',
     "a Chinese auto mechanic of 35, medium build, wearing oil-stained work pants, oil stains on his hands, a plain honest face with the straightforward expression of a man who speaks his mind, an old motorcycle nearby. A blunt loyal friend from vocational school days", False),
    ('ND-C035', '赵铁嘴·赵建国', 'BG_0044', '65',
     "a Chinese old suona master of 65, hard-bodied and straight, a dark-red weather-beaten face, long white longevity eyebrows, a thick callus on his thumb from forty years of playing the suona, wearing a thick cotton-padded winter jacket, holding his suona horn. A man of few words, every one of them true", False),
    ('ND-C036', '田大庆', 'BG_0045', '58',
     "a Chinese mahjong-parlor owner of 58, short and plump, hair neatly combed, wearing a green checkered apron over his shirt, standing among mahjong tables with tiles, a talkative friendly face with an excellent memory, the parlor's small back room behind him", False),
    ('ND-C037', '冯秀珍', 'BG_0046', '52',
     "a Chinese woman of 52 who looks a few years older than her age, neatly dressed, soft-spoken with quiet tired dignity in her eyes, in a modest county-town apartment. The ex-wife of a man whose secrets she could never ask out", False),
]
# ND-C032 赵德明 handled separately with a custom vintage-photo prompt.


def extract_block_text():
    src = INK.read_text(encoding='utf-8')
    blocks = re.split(r'\n(?==== BG_)', src)
    by_id = {}
    for b in blocks:
        m = re.match(r'=== (BG_\d+) ===', b)
        if m:
            by_id[m.group(1)] = b.strip()
    return by_id


def section_of(block_text, block_id):
    """Grab the 外貌-ish section: 【外貌/美术参考】, bare 外貌/美术参考, or 外貌：line."""
    m = re.search(r'【外貌/美术参考】\s*\n?(.*?)(?=\n【|\Z)', block_text, re.S)
    if not m:
        m = re.search(r'^外貌/美术参考\s*\n(.*?)(?=\n[A-Z一-鿿]+\n|^核心|\n- \(返回\)|\Z)', block_text, re.S | re.M)
    if m:
        return re.sub(r'\n\s*$', '', m.group(1)).strip()
    # last resort: single 外貌：line
    for line in block_text.split('\n'):
        if line.strip().startswith('外貌：'):
            return line.strip()
    return None


def main():
    blocks = extract_block_text()
    chars = []
    for cid, name, block, age, body, derived in CHARACTERS:
        excerpt = section_of(blocks[block], block)
        prompt = PREFIX + body + TAIL
        chars.append({
            'id': cid, 'name': name, 'block': block, 'age': age,
            'source_excerpt': excerpt, 'derived_from_story': derived,
            'prompt': prompt, 'status': 'pending',
        })
    # ND-C032 赵德明: deceased Cultural Revolution worker leader, exists only as a
    # grave and in old men's memories -- a vintage black-and-white photograph render.
    c032 = {
        'id': 'ND-C032', 'name': '赵德明', 'block': 'BG_0039', 'age': '已故(60年代)',
        'source_excerpt': section_of(blocks['BG_0039'], 'BG_0039'),
        'derived_from_story': False,
        'prompt': ("Photorealistic 3D render in the style of a faded vintage 1960s "
                   "black-and-white photograph with film grain and light scratches: a Chinese "
                   "steel-plant worker leader photographed in the 1960s, resolute square-jawed "
                   "face, thick brows, wearing a blue worker's cap and a cotton worker's uniform "
                   "of the Cultural Revolution era, standing before the factory gate with workers "
                   "behind him. A hero of workers remembered only in old men's memories and a "
                   "grave on the hill. --ar 2:3 --raw --v 8.1 --relax --no color, painting, "
                   "illustration, brushstrokes, cartoon, miniature, interface, text, watermark, "
                   "extra people, deformed hands, extra fingers"),
        'status': 'pending',
    }
    chars.append(c032)
    # keep canonical order C001..C037
    order = ['ND-C%03d' % n for n in range(1, 38)] + ['ND-C008B']
    chars.sort(key=lambda c: order.index(c['id']) if c['id'] in order else 999)
    record = {
        'schema': 'northeast-disco-mj-characters-v1',
        'created': '2026-09-17',
        'based_on': 'content/story/ink/背景设定.ink',
        'direction': 'user_selected_20260917',
        'direction_notes': [
            '全员 37 个角色各配肖像资产（C008 父亲含壮年/晚年两张，共 38 个任务）。',
            '沿用 40 景雪夜批次的方向：3D写实、UE5质感、Raw、V8.1、Relax、个性化关闭、文件夹 东北迪斯科参考图。',
            '角色肖像不用 45° 正交俯视（那是场景镜头）；改为半身肖像，画幅 2:3（与现有角色图 dong.png 1024x1536、grandmother-reference.png 896x1344 一致）。',
            '无外貌段落的角色（C028 朱启渲 / C029 赵淑兰 / C030 李长胜 / C031 王淑英 / C033 孙翠花）由剧情角色推导外貌，已在记录中标注 derived_from_story。',
            'C032 赵德明（已故文革工人领袖）单独用 60 年代黑白老照片风格渲染；C028/C031 为医院病床状态。',
            '生成结果人审后接入，不自动进游戏。',
        ],
        'style_basis': {
            'scene_record': '美术概念/MJ剧情场景40景雪夜_v1_20260917.json',
            'style': ('Atmospheric photorealistic 3D character portrait, Unreal Engine 5 visual '
                      'quality, realistic skin, physically based surface shading, cinematic soft '
                      'lighting -- same direction as the user-selected scene style minus the '
                      'orthographic camera.'),
        },
        'folder': {'id': 'd1ff8d95-e403-45ba-8f03-6991d740a0ce', 'name': '东北迪斯科参考图'},
        'params': {'ar': '2:3', 'raw': True, 'v': '8.1', 'relax': True,
                   'personalization': False, 'style_references': []},
        'generation_status': 'prompts_built_pending_submission',
        'source_hashes': {
            'content/story/ink/背景设定.ink':
                hashlib.sha256(INK.read_bytes()).hexdigest(),
        },
        'characters': chars,
    }
    OUT.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'wrote {OUT} with {len(chars)} entries')
    for c in chars:
        print(c['id'], c['name'], len(c['prompt']), 'excerpt' if c['source_excerpt'] else 'NO-EXCERPT')


if __name__ == '__main__':
    main()
