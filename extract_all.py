import re

swf_path = r"C:\Users\primetime43\Downloads\Blue Slide Park Game\Mac Miller Blue Slide Park Game\BlueSlidePark-game\test\slide~.swf"

with open(swf_path, 'rb') as f:
    data = f.read()

# Extract all printable ASCII strings >= 6 chars
strings = []
current = b''
start = 0
for i, b in enumerate(data):
    if 32 <= b < 127:
        if not current:
            start = i
        current += bytes([b])
    else:
        if len(current) >= 6:
            strings.append((start, current.decode('ascii', errors='replace')))
        current = b''
if len(current) >= 6:
    strings.append((start, current.decode('ascii', errors='replace')))

print(f"Total strings found: {len(strings)}")
print()

# Categorize interesting strings
urls = []
game_text = []
assets = []
config = []
developer = []
music = []
facebook = []
other_interesting = []

for offset, s in strings:
    sl = s.lower()

    # Skip Unity/Flash engine internals and file paths
    if any(x in sl for x in ['converteddotnetcode', 'stagingarea', 'cycript', 'cyctype',
                               'cycmember', 'cycclass', 'cycobject', 'cycmethod',
                               'cycfield', 'cycproperty', 'cycparameter', 'cyclocal',
                               'cycgeneric', 'cycnamespace', 'cycattribute',
                               'system.', 'unityengine.', 'flash.', 'adobe.',
                               'actionscript3', 'builtin', 'serialize', 'deserialize',
                               'remappptrs', 'constructor', 'cil2as', 'flashsupport',
                               'platformdependent', 'buildagent', 'mscorlib',
                               'getcomponent', 'setactive', 'findchild', 'nguitools',
                               'uilabel', 'uisprite', 'uipanel', 'uiwidget',
                               'tweenposition', 'tweenscale', 'tweenalpha',
                               'tweencolor', 'tweenrotation', 'itween_',
                               'springpanel', 'dragpanel', 'uibutton',
                               'uicheckbox', 'uiinput', 'uipopup', 'uitext',
                               'uiscroll', 'uislider', 'uigrid', 'uitoggle',
                               'uianchor', 'uicamera', 'uidrag', 'uifilled',
                               'uitable', 'uisaved', 'invstat', 'invequip',
                               'invattach', 'invbase', 'invgame', 'invdatabase',
                               'networkidentity', 'networkbehaviour', 'networkmanager',
                               'networkserver', 'networkclient', 'networktransform',
                               'syncvar', 'clientrpc', 'command_', 'targetrpc',
                               'spawnable', 'unetplayer', 'playercallbacks',
                               'spawningbase', 'noauth', 'playerprefs',
                               'monobehaviour_', 'gameobject_', 'transform_',
                               'component_', 'object_', 'debug_', 'application_',
                               'mathf_', 'vector3_', 'quaternion_', 'color_',
                               'time_', 'input_', 'screen_', 'physics_',
                               'renderer_', 'collider_', 'rigidbody_', 'camera_',
                               'light_', 'animation_', 'audiolistener_',
                               'audiosource_', 'particleemitter_', 'particle',
                               'mesh_', 'skinned', 'texture_', 'material_',
                               'shader_', 'render', 'gui_', 'guilayout',
                               'guistyle', 'guitext', 'guitexture',
                               '$type', '_$type', 'cycarray', 'cycvalue']):
        continue

    # URLs
    if 'http' in sl or 'www.' in sl:
        urls.append((offset, s))
    # Facebook related
    elif 'facebook' in sl or 'fb_' in sl or 'tweet' in sl or 'twitter' in sl:
        facebook.append((offset, s))
    # Music/sound references
    elif any(x in sl for x in ['.mp3', '.wav', '.ogg', 'music', 'song', 'audio', 'sound']):
        music.append((offset, s))
    # Game text (UI strings, messages)
    elif any(x in sl for x in ['most dope', 'my team', 'score', 'press ', 'click ',
                                  'enter ', 'type ', 'your name', 'game over',
                                  'retry', 'play', 'start', 'prize', 'claim',
                                  'congrat', 'level', 'high score', 'best',
                                  'points', 'welcome', 'slide', 'blue']):
        game_text.append((offset, s))
    # Asset/prefab names
    elif any(x in sl for x in ['.prefab', 'prefab', 'ice_cream', 'icecream',
                                  'obstacle', 'obsticle', 'victory', 'tree',
                                  'building', 'mac_', 'player', 'slide',
                                  'camera', 'piece']):
        assets.append((offset, s))
    # Developer/build info
    elif any(x in sl for x in ['pla', 'studio', 'v21', 'version', 'build',
                                  'developer', 'author', 'copyright', 'vnetrix', 'umg']):
        developer.append((offset, s))
    # Config values and game logic
    elif any(x in sl for x in ['speed', 'spawn', 'angle', 'death', 'bonus',
                                  'multiplier', 'difficulty', 'random', 'chance',
                                  'width', 'height', 'radius', 'distance',
                                  'timer', 'cooldown', 'interval', 'threshold',
                                  'lerpspeed', 'maxspeed', 'minspeed']):
        config.append((offset, s))
    # Leaderboard/score/hash related
    elif any(x in sl for x in ['hash', 'sha1', 'salt', 'secret', 'key',
                                  'leaderboard', 'rank', 'submit', 'upload',
                                  'fetch', 'poke', 'request', 'response',
                                  'user_id', 'userid', 'postscor']):
        other_interesting.append((offset, s))
    # Specific game class names
    elif any(x in sl for x in ['slidecontroller', 'slidermovement', 'scoremanager',
                                  'deadmenu', 'startmenu', 'camfollow', 'textentry',
                                  'slidemesh', 'slidepiece', 'victoryball',
                                  'obsticlecollider', 'externalcall', 'httppoke',
                                  'leaderboard']):
        other_interesting.append((offset, s))

# Print results
sections = [
    ("URLS & ENDPOINTS", urls),
    ("GAME TEXT & UI STRINGS", game_text),
    ("ASSET/PREFAB NAMES", assets),
    ("MUSIC/SOUND REFERENCES", music),
    ("GAME CONFIG & LOGIC", config),
    ("DEVELOPER/BUILD INFO", developer),
    ("FACEBOOK/SOCIAL", facebook),
    ("LEADERBOARD/HASH/NETWORK", other_interesting),
]

for title, items in sections:
    if items:
        print(f"{'=' * 60}")
        print(f"  {title} ({len(items)} found)")
        print(f"{'=' * 60}")
        seen = set()
        for offset, s in items:
            # Deduplicate
            if s not in seen:
                seen.add(s)
                print(f"  [{offset:>10}] {s[:150]}")
        print()
