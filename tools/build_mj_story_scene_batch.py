"""Build a source-linked first-pass scene-design batch; this script never submits MJ jobs."""
from pathlib import Path
from hashlib import sha256
import json

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '美术概念'
INK = 'content/story/ink/'
QIAOSI = 'design_docs/巧思笔记/'
MAP = 'content/story/source-documents/场景地图.md'
ART = 'design_docs/美术场景需求_MJ提示词.txt'
D0ART = 'design_docs/D0_出图包_3D建模参考.md'
SCENES = []

def scene(name, day, source, anchor, family, kind, beat, layout, action, light, people=True):
    SCENES.append(dict(name=name, day=day, source=source, anchor=anchor, spatial_family=family,
        kind=kind, story_beat=beat, layout=layout, action=action, light=light, people=people))

scene('公交车站·董点事归乡', 'D0', INK+'D0_序章.ink', 'A000-公交车站', 'bus-stop', 'exterior',
      '归乡入口；预言者第一次遇见玩家。',
      'A small Northeast Chinese town bus stop beside a two-lane road. Corrugated metal shelter, two concrete benches, rusted stop pole, faded timetable board, dusty paving and summer weeds. A worn bus pulls away at the far edge. Keep a clear pedestrian route from shelter to the town street.',
      'One young Chinese man with a small travel bag pauses by the bench; one elderly Chinese woman with a black walking cane sits watching him, her hand slightly extended toward him.',
      'Early summer morning with thin pale mist in the distance, sparse warm tones in the shelter; foreground people, bus stop and route remain clear.')

scene('便利店废墟·一楼调查', 'D0–D1', INK+'D0_序章.ink', 'A001_便利店', 'family-store', 'interior',
      '玩家进入母亲遇害的小店；柜台、货架和地面痕迹构成调查入口。',
      'An abandoned burned family convenience store. Entrance at front right, wooden glass-top counter at the back with a stopped round wall clock, metal shelf skeletons along the left wall and one collapsed central shelf row. A narrow stair in the rear corner leads upstairs. Charred plaster, ash and glass on the floor, a loosened floorboard at the opposite corner. Preserve the main aisle and all walls in readable solid 3D.',
      'One young Chinese man crouches beside a scorched shelf support, looking closely at the floor with one hand braced on his knee. No visible body.',
      'Pale daylight from the shopfront and damaged ceiling, cool shadows, small amber highlights, no fog.')

scene('便利店二楼·母亲旧物', 'D0–D1', INK+'D1_第一天.ink', '一个布老虎', 'store-upstairs', 'interior',
      '旧纸箱中的布老虎与母子合影，承接私人回忆线索。',
      'A cramped abandoned upstairs dwelling above a small Chinese shop. Narrow wooden stair reaches a tiny landing, a simple bed along the left wall, old wardrobe at the back, cardboard box on top of the wardrobe, small bedside cabinet, dusty window partly covered with old newspaper. Inside an opened box are a handmade cloth tiger toy and a faded mother-and-child photograph.',
      'One young Chinese man stands beside the wardrobe holding the small photograph carefully; the open box and cloth tiger stay clearly visible.',
      'Soft afternoon light through the partly covered window, dusty muted blue shadows and warm paper tones.')

scene('便利店门口与侧巷·白天', 'D0–D1', INK+'D1_第一天.ink', '隔壁的门开了', 'store-exterior', 'exterior',
      '邻居目击证词发生在店门外；门口与侧巷必须保持连通。',
      'Exterior of a burned two-storey family shop in a Northeast Chinese small town. Two worn entrance steps, scorched doorframe and faded sign, upstairs window partly covered from inside, narrow brick alley along the right side connecting to the rear. Neighboring residential door to the left, vocational school gate glimpsed across the street, cracked paving and summer weeds.',
      'One elderly Chinese neighbor carrying a vegetable basket speaks from her doorway to one young man standing outside the shop. Keep the alley entrance visible.',
      'Clear overcast daylight, cool grey brick, subdued warm clothing accents, no fog.')

scene('父亲葬礼·稀疏的灵棚', 'D0', QIAOSI+'D0_序章_巧思笔记.md', 'D0葬礼四个具体细节', 'funeral-lot', 'exterior',
      '一口棺材、稀疏吊唁者和唢呐班，建立D0与D7的呼应。',
      'A modest funeral shelter on an open lot near the uncles home in a small Northeast Chinese town, the burned shop visible down the street. One closed plain wooden coffin centered under a worn canvas awning, white paper wreaths and cloth strips, a portrait stand at the rear, two mostly empty rows of folding chairs, a small musicians platform at left. Entrance walkway at front right; preserve open ground between chairs and coffin.',
      'A small suona band plays on the left, a middle-aged relative receives two arriving mourners by the entry, a young Chinese man stands quietly near the chairs. All figures small within the environment.',
      'Flat overcast daytime light, pale cloth and muted earth, restrained warm brass highlights, no fog.')

scene('叔叔家·葬礼后的饭桌', 'D0', D0ART, 'SC-D0-21', 'uncle-dining', 'interior',
      '葬礼后的家庭晚饭；墙上兄弟合影进入玩家视野。',
      'An ordinary Northeast Chinese family dining room connected through an open doorway to a small kitchen. Square wooden dining table at center with simple vegetable dishes, rice bowls and enamel cups; worn sofa against left wall, old CRT television opposite, framed photograph of two young factory-worker brothers on the rear wall. Keep doorway, table approach and seating paths clear.',
      'A young man and his middle-aged uncle sit quietly across the table; an aunt sets a cup of water beside the young man. Faces remain secondary to the room and the family photograph.',
      'One warm ceiling bulb at night, amber table island, deep muted teal corners, no supernatural effects.')

scene('董点事卧室·夜间旧物', 'D0–D1', D0ART, 'SC-D0-22', 'dong-bedroom', 'interior',
      '睡前回忆与随身听触发空间；物件需留出可检查位置。',
      'A small upstairs guest bedroom in a working-class Northeast Chinese home. Single bed against left wall, narrow wooden desk beneath the rear window, old wardrobe at right, bedside table holding a portable cassette player and a cassette. Faded football poster, folded clothing and an old framed family photograph. Door at front right opens to a narrow landing.',
      'One young Chinese man sits on the bed edge, leaning forward to pick up the cassette player. His feet rest on the floor and the desk remains unobstructed.',
      'Warm bedside lamp and cool night window light, quiet intimate painted shadows.')

scene('省道修车棚·一起抬轮胎', 'D1', INK+'D1_第一天.ink', '帮叔叔抬轮胎', 'repair-shop', 'interior',
      '叔叔与玩家共同劳动，建立人物关系与修车店困境。',
      'A corrugated metal roadside motorcycle and truck-tire repair shed in Northeast China, open front facing the road. Tool rack at rear left, two motorcycles on the right, stacked tires against the back, small wooden table with enamel mugs and radio near the left entrance. A clear central work bay contains one large upright truck tire; oil-stained concrete and practical worn tools.',
      'A middle-aged mechanic uncle and a young Chinese man lift the same heavy truck tire together, both gripping its sides with bent knees. A second mechanic works quietly by the tool rack.',
      'Summer morning daylight through the open front, warm skin and rust accents against cool steel.')

scene('叔叔家院子·家与修车店之间', 'D1–D6', MAP, '| S01 | 叔叔家', 'uncle-yard', 'exterior',
      '家庭生活与调查往返的稳定节点；保留厨房、住处、外出通道。',
      'A small enclosed Northeast Chinese family courtyard. Single-storey kitchen on the left with lower white ceramic tiles, main dwelling at the back, gate to the street at front right, worn stone entrance steps under an amber porch bulb. Laundry line, washbasin, aluminum pots, one dry potted plant and two low stools. Continuous empty walking space links gate, kitchen and house.',
      'An aunt carries an enamel basin from the kitchen while the mechanic uncle washes his hands at the courtyard tap.',
      'Soft summer afternoon daylight, white tile and grey brick with restrained warm highlights.')

scene('小虎房间·表弟的沉默', 'D1', INK+'D1_第一天.ink', '叔叔出门了，就你和表弟', 'xiaohu-bedroom', 'interior',
      '家庭证词线的私密对话空间；图中只表现正常交谈。',
      'A compact teenage boys bedroom in an ordinary Chinese workers home. Single bed along the left, small school desk beneath a rear window, exercise books and closed schoolbag on the desk, plain wardrobe on the right, modest sports poster. Door stays open toward the family corridor, clear floor between doorway and bed.',
      'One fully clothed teenage cousin sits on the bed looking down at his hands; his young adult cousin sits on a wooden chair near the doorway listening calmly. Ordinary family conversation.',
      'Gentle morning window light, muted blue-green room with soft warm skin tones.')

scene('派出所·白天办公与走廊', 'D1', INK+'D1_第一天.ink', '镇公安局。一栋三层的旧楼', 'police-office', 'interior',
      '李正进入工作日常；办公区、接待与档案门形成清楚的空间关系。',
      'Small-town Chinese police station cutaway: straight pale-green corridor at front, reception window on left, main office on right with two facing desks piled with folders, enamel mugs, old telephone and desk lamp. Iron archive door at far corridor end, adjacent meeting room glimpsed with district wall map. Terrazzo floor, worn beige plaster, simple metal cabinets.',
      'One adult Chinese investigator sorts a paper case file at his desk while another officer passes the corridor holding a folder. Keep furniture and people within their rooms.',
      'Daylight from office windows mixed with cool fluorescent tubes, restrained amber desk-lamp accents.')

scene('档案室·旧案检索', 'D2–D5', INK+'D2_第二天.ink', 'B018_派出所\n', 'police-archive', 'interior',
      '旧案与户籍记录的检索空间；纸张为构图重点，不依赖生成文字作证据。',
      'A locked archive room behind an iron door in a small Chinese police station. Three rows of grey metal filing cabinets create two narrow straight aisles; a worn reading table at front with several opened case folders, document envelopes and an old desk lamp. Small high window at the rear, dust on cabinet tops, no modern computer wall.',
      'One middle-aged Chinese investigator stands at the reading table comparing two paper files, with an open cabinet drawer behind him. The entry door and both cabinet aisles stay visible.',
      'Late evening desk-lamp pool over warm paper, cool green-grey metal receding into shadow.')

scene('李正家·噩梦醒来的清晨', 'D1', INK+'D1_第一天.ink', '你猛地从床上坐起来', 'li-home', 'interior',
      '李正与妻子的清晨，表现噩梦后的动作而非噩梦尸体。',
      'A modest Chinese police investigators bedroom connected to a tiny hallway. Double bed on left, bedside cabinet with a glass of water and alarm clock, wardrobe along back wall, shirt hanging on a chair, family photograph on the cabinet. Open door at front right reveals the edge of a small breakfast table in the next room.',
      'A fully clothed adult man sits upright at the edge of the bed with one hand on his forehead; his fully clothed wife stands beside the bed offering him water. Keep the room the main subject.',
      'Pale early morning window light, muted teal shadows and warm domestic wood, no dream apparition.')

scene('王氏中医馆·前厅接诊', 'D1', INK+'D1_第一天.ink', '脉象异常平稳', 'wang-clinic-front', 'interior',
      '王少鹏的身份表演与诊所日常；前厅和后屋入口需分明。',
      'A small traditional Chinese medicine shopfront in Northeast China. Wooden medicine drawers cover the rear wall, consultation desk at center with wrist cushion and prescription papers, two patient chairs, faded cloth pennants and framed certificates, old refrigerator tucked at left. Fabric curtain at rear right leads to a separate back room; narrow stair at right climbs to the apartment.',
      'One adult Chinese practitioner in ordinary modest clothing sits taking the wrist of one elderly female patient across the desk, while a young assistant carries a small packet toward the drawers.',
      'Afternoon shopfront daylight, warm wooden drawers, pale green shadows behind the curtain.')

scene('诊所后屋·守着奶奶', 'D1–D6', INK+'D2_第二天.ink', '【后屋——帘子后面】', 'wang-clinic-back', 'interior',
      '奶奶病房是王少鹏线的私人责任空间。',
      'A cramped private sickroom behind a fabric curtain in a Chinese neighborhood clinic. One hospital bed along left wall, bedside cabinet with tissues, medicine bottles and a faded family photograph, small bedside monitor and IV stand, curtain-drawn window at back. Entry curtain at front right and a chair next to the bed; keep tubing simple and contained.',
      'An elderly woman rests fully covered by a clean blanket; one adult grandson sits in the bedside chair holding her hand. No exposed injury or treatment procedure.',
      'Night, soft green monitor light balanced with a small warm bedside bulb, deep quiet blue-green shadows.')

scene('王家卧室·枕边出现纸条', 'D5', INK+'D5_第五天.ink', '你的目光移到枕头旁边', 'wang-apartment', 'interior',
      '原文纸条出现在枕边；维持卧室私密性与清楚的发现动作。',
      'A modest upstairs bedroom above a Chinese clinic. Double bed along the left wall, plain wardrobe on right, bedside cabinet holding a glass of water, small desk under the rear window. Door at front right leads to the landing; faded red wedding paper decoration near it. A single small folded white note beside a pillow has only a triangle and a circle drawn on it.',
      'An adult Chinese man wearing plain pajamas sits at the bed edge carefully lifting the note from beside his pillow; his adult wife sleeps under a blanket on the other side of the bed. Keep the complete room and door route visible.',
      'Soft morning window light, faded wedding reds against muted teal plaster and warm wood.')

scene('面馆·午饭与闲话', 'D1–D2', INK+'D1_第一天.ink', '面馆', 'noodle-shop', 'interior',
      '用做饭和吃饭动作表现日常信息交换。',
      'A tiny Northeast Chinese noodle shop with four square wooden tables and simple benches, narrow cooking counter across the rear, large steaming noodle pot, stacked bowls and condiments. Front window looks onto the main street, entrance at front right, clear aisle between tables. Worn white tiles, steam near the pot, plain menu board with indistinct marks.',
      'A middle-aged female owner in an apron lifts noodles from the pot with a long strainer; two elderly men sit chatting over bowls by the window. One young man eats at a separate table.',
      'Warm midday window light and amber cooking-area glow, soft steam with clearly readable room edges.')

scene('主街·七月白天的日常', 'D1', INK+'D1_第一天.ink', '空荡荡的街道', 'main-street', 'exterior',
      '导航主场景：店面入口、路口和步行路线要清楚。',
      'A continuous stretch of main street in a declining Northeast Chinese industrial town in July. Cracked concrete road runs diagonally through the frame, two-storey red-brick and grey plaster shopfronts on both sides, utility poles with sagging wires. Noodle shop and repair entrance near a large locust tree junction, shuttered shops farther along, distant factory chimney. Keep connected sidewalks and door approaches readable.',
      'A few elderly residents sit on stools by their doors, a young man walks along the sidewalk, one shopkeeper lifts a roller shutter. People remain small relative to the architecture.',
      'Overcast summer daylight, dusty green foliage, faded blue-grey walls and restrained warm shop windows.')

scene('主街·雾中的归路', 'D2–D6', QIAOSI+'D2_第二天_巧思笔记.md', '雾重新变厚', 'main-street', 'exterior',
      '同一主街的夜间雾态；保留路口和店门识别点。',
      'The same Northeast Chinese town street layout: diagonal cracked road, two-storey brick shops, locust tree at the central junction, repair entrance and noodle shop at the near side. Moist road surface catches amber streetlights. Layered low fog partly veils distant buildings but leaves the foreground sidewalk, doorway spacing and junction readable.',
      'One adult Chinese figure walks home along the near sidewalk carrying a small bag. Several shutters are closed; no crowd.',
      'Night with warm sodium streetlight pools fading into cool blue-green fog, readable foreground geometry.')

scene('东山坟地·父亲的新坟', 'D1', MAP, '| S09 | 东山坟地', 'east-hill-cemetery', 'exterior',
      '父亲坟地与更早的墓区并置，给家族历史留出探索空间。',
      'A small hillside cemetery above a Northeast Chinese town. Narrow dirt footpath climbs from front right to a fresh modest grave with plain stone marker and white paper flowers. Older weathered graves continue uphill among summer grass; low retaining stones and scattered trees, distant factory rooftops below. Markers carry indistinct inscriptions only.',
      'One young Chinese man crouches at the fresh grave, setting down a small bunch of flowers with both hands. No other visitors nearby.',
      'Soft overcast afternoon light, subdued green grass and warm earth, distant blue-grey industrial skyline.')

scene('钢铁厂·紧锁的大门', 'D1', INK+'D1_第一天.ink', '大门紧锁', 'steelworks-gate', 'exterior',
      '工厂仍在运作却不让人进入；本轮只设计外围。',
      'Entrance perimeter of an aging Northeast Chinese steelworks: closed double steel gate, small brick guard booth, tall boundary wall, pedestrian approach from a cracked service road. Beyond the locked gate only factory rooflines, pipes and a tall smoking chimney are visible. Summer weeds along the wall, faded notice board without legible text.',
      'One young Chinese man with a shoulder bag stands on the public pavement looking through the gate; a guard is visible inside the booth.',
      'Cloudy summer afternoon, pale chimney smoke against grey sky, rusty steel and muted green weeds.')

scene('镇西火场·封锁后的民房', 'D2', INK+'D2_第二天.ink', '镇西', 'west-fire-house', 'exterior',
      '保留独立火场身份，不把不同文本中的起火地点或死伤者合并。',
      'A recently burned single-storey brick house on the west edge of a Northeast Chinese town. Soot-blackened doorway, damaged window frames, wet scorched walls and scattered roof tiles. Police cordon spans the front yard, access lane enters from front left, neighboring intact house at right. Show the whole exterior footprint and the relationship between door, lane and boundary fence.',
      'Two adult investigators examine the doorway from outside the cordon while a resident waits farther back. No victims, no active flames.',
      'Grey morning light, damp ground and thin distant summer fog, restrained warm brick under cold soot.')

scene('县医院·探望病人', 'D2', INK+'D2_第二天.ink', '医院', 'county-hospital', 'interior',
      '调查中的住院探访；伤情仅以包扎和病床表示。',
      'An older county hospital ward in Northeast China. One bed along the left wall, metal bedside cabinet, IV stand, small visitor chair, curtained rear window, pale green lower walls and worn cream floor tiles. Door at front right opens onto a narrow corridor; preserve clear access on both sides of the bed.',
      'An adult patient rests fully covered by a white blanket with clean bandages visible on an arm; an adult police investigator sits beside the bed listening. No exposed wounds or medical procedure.',
      'Overcast window light mixed with cool fluorescent ceiling light, a small warm patch on the bedside wood.')

scene('县医院·空下来的病床', 'D4', INK+'D4_第四天.ink', '任务名称：朱启渲死讯', 'county-hospital', 'interior',
      '以病人死讯为依据设计事后空床变体；探访空床是环境设计提案，不宣称原文必有此镜头。',
      'The same small county hospital ward: single bed along the left wall, metal cabinet and IV stand, visitor chair, curtained rear window, door at front right, pale green lower walls. The bed is empty with sheets freshly folded and pillow removed. A few personal belongings sit in a plain paper bag on the bedside cabinet.',
      'An adult Chinese investigator stands just inside the doorway looking at the empty bed; a nurse carries a folded sheet into the corridor.',
      'Flat grey daytime light, silent cool fluorescent interior with a small warm reflected window edge.')

scene('退休警员家·旧事访谈', 'D2', MAP, '| S15 |', 'retired-officer-home', 'interior',
      '依场景地图补足旧案访谈空间；不把退休警员误认作陈国涛。',
      'A modest retired Chinese police officers sitting room: worn wooden armchairs facing each other across a low table, enamel tea cups, newspaper rack, old cabinet with file envelopes and family photographs. Entrance at front right, small rear window and doorway into a kitchen. Keep the table and passage separated.',
      'An elderly Chinese man sits with a tea cup while an adult investigator opposite him takes notes in a small notebook. A plain old file envelope lies closed on the table.',
      'Soft afternoon window light, warm aged wood, muted teal plaster and gentle shadow.')

scene('河堤·上涨的河水', 'D2', INK+'D2_第二天.ink', '河水比昨天', 'riverbank', 'exterior',
      '河水变化是天气与局势发展的可见线索，岸上通路仍要清楚。',
      'A riverside embankment beside a small Northeast Chinese industrial town in July. Raised dirt walking path runs diagonally, sloped concrete bank descends to visibly high muddy water close to the lower steps. Short stone stair connects the upper road to a safe fishing platform, willow branches at rear, distant low factory silhouettes. Foreground path remains clear and dry.',
      'One elderly fisherman sits well back from the waters edge with his rod, while a young man pauses beside him on the upper path.',
      'Cool overcast afternoon with low fog over distant water, green summer foliage and faint amber reflections.')

scene('老槐树·傍晚的预言者', 'D2', INK+'D2_第二天.ink', '老槐树', 'locust-tree', 'exterior',
      '预言者谈话节点；座位、树、街口组成可重复辨认的空间。',
      'A large old locust tree at a Northeast Chinese town street junction, summer leaves overhead. Low circular brick tree surround with a worn wooden chair beside it, cracked pavement and two narrow streets receding behind. Old brick shop wall at left, one amber streetlamp at right, clear pedestrian route around the tree.',
      'An elderly Chinese woman sits on the chair with a black walking cane across her knees, raising one hand while speaking to a standing adult Chinese man in modest clinic clothes.',
      'Summer dusk, amber lamp just switched on, cool teal fog beginning beyond the junction while foreground remains clear.')

scene('职高校门·门卫回忆双胞胎', 'D5', INK+'D5_第五天.ink', '门卫室。一个老头坐在里面', 'vocational-gate', 'exterior',
      '门卫口述双胞胎的旧事；保留岗亭窗口、茶缸、电视和老花镜。',
      'Entrance of an aging Chinese vocational school: rusted sliding gate, low brick guard room at left with an open service window, paved approach and a narrow pedestrian gap. Behind the gate a worn basketball court and plain school block are visible, weeds along the fence. Inside the booth a small old television shows static beside a table with an enamel tea mug and worn magazine.',
      'An elderly guard removes and wipes his reading glasses while a young adult Chinese visitor stands outside the window listening. Keep both figures small within the full gate scene.',
      'Overcast summer afternoon with soft fog inside the distant campus, dusty green weeds, muted red brick and warm light from the guard room.')

scene('职高楼顶·封闭校园的痕迹', 'D2', INK+'D2_第二天.ink', '天台', 'vocational-roof', 'exterior',
      '设计文本中被锁住的天台与痕迹；这是环境展示，不能据此解锁玩家通路。',
      'Flat rooftop of an old Chinese vocational school with waist-high concrete parapet, a small stairwell access hut and a weathered steel door, drainage outlet and scattered old cigarette ends near the hut. Full rooftop footprint visible, neighboring school roofs and overgrown court beyond. Clear central walking area and an unobstructed route to the stair door.',
      'Empty rooftop, no people. The steel access door remains firmly closed and locked; small recent cigarette ends lie near its lower edge.',
      'Cloudy summer daylight with distant blue-grey haze, faded warm concrete and muted green courtyard below.', False)

scene('婚礼正厅·喜宴中的疏离', 'D3', INK+'D3_第三天.ink', '流水席主场地：红桌红椅', 'wedding-hall', 'interior',
      '婚礼主场保留宴席、舞台与偏房入口，人物规模服从空间阅读。',
      'A small-town Chinese wedding banquet hall with twelve round dining tables, plain red chair covers and a modest low stage at the rear with faded red fabric decoration. Main entrance at front, side-room doorway clearly visible on the right, serving passage along left wall. Table spacing forms a clear central aisle; paper decorations have no legible text.',
      'Small groups of adult guests sit unevenly around the tables; a bride and groom stand near the low stage while two musicians perform at its side. Keep individuals small and the hall layout dominant.',
      'Warm overhead bulbs mixed with grey summer window light, restrained red accents over muted teal shadows.')

scene('婚礼偏房·刘小红的谈话', 'D3', INK+'D3_第三天.ink', '偏房：饭店旁一间小屋', 'wedding-side-room', 'interior',
      '偏房与正厅形成空间反差；只呈现成人之间的谈话。',
      'A plain side room beside a small-town Chinese wedding hall. Worn sofa against the rear wall, beer crates stacked at left, low table with two cups and a few bottles, open door at front right revealing warm banquet lights. Bare plaster and old concrete floor, keep the route from door to sofa clear.',
      'A fully clothed adult Chinese woman with short hair and plain everyday clothes sits at one end of the sofa holding a cup; an adult visitor sits on a separate chair listening.',
      'A weak warm ceiling bulb and warmer spill from the wedding hall, cool teal shadows in the corners.')

scene('便利店侧巷·D3夜间空景', 'D3', INK+'D3_第三天.ink', '地点：便利店旁的胡同', 'family-store-alley', 'exterior',
      '设计分支发生前的场地与可见退路；不把侵害行为作为出图内容。',
      'An empty narrow alley beside a burned two-storey Chinese convenience store. Scorched brick wall on left, intact neighboring wall on right, small service doorway and stacked empty crates set back from the route. Alley opens visibly onto the main street in front and a small rear yard beyond, continuous clear walking strip between them.',
      'Empty environment, no people. A discarded ordinary paper cup lies beside the wall; no violent event is depicted.',
      'Night, a single amber streetlamp at the alley mouth, dim blue-green fog farther down the lane, wet concrete catches a narrow reflection.', False)

scene('宏大地产·谈判桌上的地块', 'D1–D4', INK+'D1_第一天.ink', '宏大', 'property-office', 'interior',
      '由剧情中的地产关系设计办公谈判空间；桌面资料不生成可读假证词。',
      'A small Chinese property developers office occupying an old town shopfront. Heavy desk at rear with two visitor chairs, sofa along left wall, cheap landscape print and a large site-plan board behind the desk, metal file cabinet at right. Several rolled plans and closed contract folders on the desk; front door has a direct clear approach. Documents contain indistinct marks only.',
      'Two adult Chinese men sit across the desk, one holding down the edge of a rolled site plan with his palm while the other listens.',
      'Cloudy afternoon shopfront light, warm imitation wood furniture, cool teal wall shadows.')

scene('刘小红母亲家·重逢后的谈话', 'D4–D5', MAP, '| S17 |', 'liu-mother-home', 'interior',
      '依据场景地图设计母女谈话空间；具体家具属于设计补充。',
      'A small working-class Chinese home sitting room: low sofa against the back wall, wooden table with two bowls and a thermos, sewing basket beside a chair, old family photographs on a cabinet, faded curtain at rear window. Front door at right and bedroom doorway at left, unobstructed route between them.',
      'A fully clothed adult Chinese woman with short hair sits across the table from her elderly mother; the mother places a bowl near her daughters hands. Both remain seated.',
      'Soft late-afternoon window light, warm domestic wood and faded teal fabric, no fog indoors.')

scene('修车铺门前·冲突前的站位', 'D5', INK+'D5_第五天.ink', '群架', 'repair-shop', 'exterior',
      '复用修车铺门前布局，表现冲突发生前的距离与出口。',
      'Roadside tire repair shed on a Northeast Chinese provincial road. Corrugated canopy over an open work bay, large truck tires stacked against the rear wall, workbench and wheeled jack at left, low stool and wash basin at right. Open concrete apron connects to the road at front; maintain a clear side exit beside the shed.',
      'Two small groups of fully clothed adult men face each other several steps apart on the apron, one older mechanic holds out a hand between them. No physical attack, weapons or injuries.',
      'Heavy overcast summer afternoon, dusty amber concrete and cool grey-green shadows beneath the canopy.')

scene('陈国涛办公室·保险箱打开', 'D5', INK+'D5_第五天.ink', '把画像取下来', 'police-chief-office', 'interior',
      '只表现已经打开后的检查，不绘制开锁方法；证据文字由游戏另行制作。',
      'A modest Chinese police chiefs office inside an old station: wooden desk in rear left, two visitor chairs, steel filing cabinet at left, a small grey wall safe at standing chest height behind a removed framed landscape painting. The painting leans against the wall below the already-open safe. Three stacks of old paper documents, a brown envelope and a USB stick are visible inside. Window with closed blinds at right, office entrance at front with a clear route to the safe. Paper contents are illegible.',
      'One adult Chinese investigator stands beside the already-open wall safe carefully examining a paper folder, keeping the office entrance route clear.',
      'Dim afternoon light filtered through closed blinds across the desk, warm wood and muted teal steel cabinets.')

scene('叔叔家院子·D6夜谈与旧照', 'D6', INK+'D6_第六天.ink', '叔叔坐在院子的石头台阶上', 'uncle-yard', 'exterior',
      '家庭私密证据层：叔叔在院子石阶上交出父亲旧照，不能被便利店调查替代。',
      'A small enclosed Northeast Chinese family courtyard in summer. Two low stone steps lead up to the main house door at rear, separate low kitchen on the left with white ceramic lower-wall tiles and a glowing window, wash basin and bicycle near the left wall, narrow gate to the street at front right. Keep a clear path from gate to steps and the same compact rectangular yard footprint.',
      'An older Chinese uncle and his adult nephew sit side by side on the stone steps; the uncle passes a small faded photograph of a factory worker to the younger man. No legible image detail is required on the photograph.',
      'Quiet summer night with pale moonlight in the yard and warm kitchen window light on their hands, subdued teal shadows.')

scene('老槐树·预言者离开后的空椅', 'D6', INK+'D6_第六天.ink', '老槐树', 'locust-tree', 'exterior',
      '以事后空椅和手杖表达失去；这是原文死亡事件的环境化设计变体。',
      'The same old locust tree at a Northeast Chinese town junction: circular low brick surround, worn wooden chair beside it, old shop wall left and amber streetlamp right. The chair is now empty, a black walking cane rests against it and a plain folded cloth lies on its seat. Foreground walking route stays open, summer leaves hang above.',
      'Two adult townspeople stand a few steps away speaking quietly while looking toward the empty chair. No body is present.',
      'Late summer dusk, one warm lamp pool against cool blue-green mist beyond the crossroads.')

scene('D7集体葬礼·低云下的灵棚', 'D7', INK+'D7_第七天.ink', '棺材', 'town-east-funeral-lot', 'exterior',
      '选择集体葬礼分支的三具棺木构图，非所有分支固定人数；早晨无雾，雨尚未落下。',
      'A temporary funeral shelter on an open lot beside a Northeast Chinese town. Plain canopy on steel poles, three closed coffins aligned under it, white paper wreaths and small portrait frames with indistinct faces. Folding stools arranged in sparse rows with an open central aisle, dirt road at front and distant hills clearly visible behind. No fixed inscription or names.',
      'Small groups of fully clothed adult mourners stand apart under the canopy; one young man pauses in the central aisle holding white flowers. All coffins remain closed.',
      'Humid July morning under heavy low clouds, clear air without fog, dry ground before the rain, muted warm earth and blue-grey sky.')

scene('便利店废墟·最终对峙构图', 'D7真相线', INK+'最终审判.ink', '环境：便利店废墟', 'family-store', 'exterior',
      '真相线终局场地设计；对峙人物站位是美术方案，不替代分支脚本。',
      'Roofless ruins of a burned family convenience store in Northeast China. Only half of a soot-blackened brick wall and one doorway frame remain standing. The old checkout counter survives at the back, rusted broken shelf remains along the left wall and a fallen central shelf row leave a clear aisle from the front-right doorway to the counter. Small withered flowers in a corner. Use the roofless condition specified for the final confrontation, while preserving the earlier stores ground footprint and checkout position.',
      'Three fully clothed adult Chinese men stand several steps apart: two near the entrance and central aisle face a third behind the old counter. Hands visible, no weapons or physical violence, environment dominates the frame.',
      'Heavy July night rain falls directly into the roofless ruins, no fog, puddles reflect cool blue-green street light, one restrained warm flashlight beam crosses the counter.')

def build():
    assert len(SCENES) == 40, len(SCENES)
    sources = {}
    errors = []
    for i, item in enumerate(SCENES, 1):
        path = ROOT / item['source']
        raw = path.read_bytes()
        source_text = raw.decode('utf-8-sig').replace('\r\n', '\n')
        offset = source_text.find(item['anchor'])
        # Ink exports repeat titles in the table of contents; cite their real body.
        while offset >= 0:
            line_start = source_text.rfind('\n', 0, offset) + 1
            if not source_text[line_start:offset].lstrip().startswith('+ ['):
                break
            offset = source_text.find(item['anchor'], offset + 1)
        if offset < 0:
            errors.append(f"{i}: {item['name']} missing {item['anchor']!r} in {item['source']}")
            continue
        item['id'] = f'ND-S{i:02d}'
        item['source_line'] = source_text[:offset].count('\n') + 1
        item['source_excerpt'] = source_text[line_start:offset+500].strip()
        item['source_sha256'] = sha256(raw).hexdigest()
        sources[item['source']] = item['source_sha256']
        camera = ('Strict 45-degree elevated isometric orthographic cutaway, roof and camera-facing walls removed, complete room footprint and doorways visible.'
                  if item['kind'] == 'interior' else
                  '45-degree elevated isometric orthographic view, complete playable area visible with connected paths, clear entrances and solid architectural depth.')
        item['prompt'] = ' '.join([
            'Disco Elysium-inspired painterly 3D game environment, visible oil brushstrokes on solid forms, muted teal-and-amber palette.',
            camera, item['layout'], item['action'], item['light'],
            '--ar 4:3 --raw --v 8.1 --relax --no text, watermark, logo, anime, pixel art, snow, medieval architecture'])
        item.update(status='prepared', job_id=None, variants=[], grouped=False, human_review='pending')
    if errors:
        raise ValueError('\n'.join(errors))
    dest = OUT/'MJ剧情场景设计_首轮40景_20260909.json'
    if dest.exists() and any(s.get('job_id') for s in json.loads(dest.read_text(encoding='utf-8'))['scenes']):
        raise RuntimeError('Existing accepted jobs must not be overwritten')
    data = dict(schema='northeast-disco-mj-story-scenes-v1', created='2026-09-09',
        folder=dict(id='d1ff8d95-e403-45ba-8f03-6991d740a0ce', name='东北迪斯科参考图'),
        design_status='first_pass_for_human_review', generation_status='not_submitted',
        reused_d0_memory_job='bb4c44e8-a423-4c81-b586-dac7ae6d8519',
        assumptions=['当前剧情为七月夏季，不套用冬季积雪。D0补充段明确写到清晨夏雾；D1白天空景取清晰状态，D2以后雾加重，D7雾散、夜间暴雨。',
                     '人物动作和核心事件依据剧情；精确家具、门位、配色和镜头为本轮设计提案。',
                     '同地点变体共享布局描述；文本独立生成不能保证几何或人物外形一致。',
                     '场景图不使用角色历史sref，不附加图片参考，不启用个性化。',
                     '概念图不代表可部署游戏资产，也不视为人工美术验收通过。',
                     '连续性待统一：前期便利店仍有二楼住所，最终审判写成仅半面墙、门框且无屋顶；本轮分别忠实表现两份文本，不擅自增加D7坍塌剧情。',
                     'D0葬礼位于叔叔家附近空地；D7集体葬礼采用镇东空地。构图呼应，不断言为同一地点。'],
        source_hashes=sources, scenes=SCENES)
    dest.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    lines = ['# 东北迪斯科：剧情场景设计首轮 40 景', '',
             '依据现有 D0—D7 剧情与场景地图，设计地点、状态、通路、道具与人物动作。40 个新设计各计划提交 MJ 一组 4 张。', '',
             'D0 母亲在便利店摆货的回忆场景沿用 [已有四张结果](https://www.midjourney.com/jobs/bb4c44e8-a423-4c81-b586-dac7ae6d8519?index=0)，不重复提交。', '',
             '**当前状态：设计已完成，40 组尚未提交。** 实际提交结果以同名 JSON 的 job_id、status 与 grouped 字段为准。', '',
             '## 设计边界', ''] + ['- '+x for x in data['assumptions']] + ['',
             '## 场景索引', '', '| 编号 | 场景 | 时段 | 布局系列 |', '|---|---|---|---|']
    for s in SCENES:
        lines.append(f"| {s['id']} | {s['name']} | {s['day']} | {s['spatial_family']} |")
    for s in SCENES:
        lines += ['', f"## {s['id']} {s['name']}", '',
                  f"剧情时段：{s['day']}。用途：{s['story_beat']}", '',
                  f"出处：[{s['source']}]({(ROOT/s['source']).as_posix()}:{s['source_line']})，匹配锚点 `{s['anchor'].strip()}`。", '',
                  '原文摘录：', '', '```text', s['source_excerpt'], '```', '',
                  '**空间设计：** '+s['layout'], '', '**人物动作：** '+s['action'], '',
                  '**光照与天气：** '+s['light'], '', '**MJ 提示词：**', '', '```text', s['prompt'], '```']
    (dest.with_suffix('.md')).write_text('\n'.join(lines)+'\n', encoding='utf-8')
    print(json.dumps(dict(designs=len(SCENES), planned_images=160, json=str(dest), markdown=str(dest.with_suffix('.md'))), ensure_ascii=False))

if __name__ == '__main__':
    build()
