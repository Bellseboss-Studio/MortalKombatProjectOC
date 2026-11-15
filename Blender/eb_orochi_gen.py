"""Generador del Orochi (boss B) — rig, malla y animaciones.

Se ejecuta DENTRO de Blender, de cero:
  exec(open("Blender/eb_orochi_gen.py").read())

Construye EB_Orochi.blend desde EB_Blockout.blend:
  · escala el humanoide de 1.733 m a 3.30 m
  · agrega el par de brazos inferior (upperarm2/forearm2/hand2 .L/.R) y el hueso jaw
  · agrega cuernos y lava emisiva en los brazos inferiores
  · 10 clips con el timing de Boss_Orochi.asset
Luego se corre Blender/eb_boss_calibrate.py para el piso.
"""

import bpy
from mathutils import Vector

BASE = "D:/Unity/MortalKombatProjectOC/Blender/EB_Blockout.blend"
OUT_BLEND = "D:/Unity/MortalKombatProjectOC/Blender/EB_Orochi.blend"
OUT_FBX = "D:/Unity/MortalKombatProjectOC/Assets/_MKRebuild/Models/EB_Orochi.fbx"
TARGET_HEIGHT = 3.30
FPS = 60

if bpy.data.filepath.replace("\\", "/") != BASE:
    bpy.ops.wm.open_mainfile(filepath=BASE)
    bpy.context.view_layer.update()
for action in list(bpy.data.actions):
    bpy.data.actions.remove(action)

ARM = bpy.data.objects["EB_Rig"]
BODY = bpy.data.objects["EB_Body"]
ARM.name = "EB_Rig_Orochi"
ARM.data.name = "EB_Rig_Orochi"

# --------------------------------------------------------------------------- escala
K = TARGET_HEIGHT / BODY.dimensions.z
bpy.ops.object.select_all(action="DESELECT")
ARM.select_set(True)
BODY.select_set(True)
bpy.context.view_layer.objects.active = ARM
ARM.scale = (K, K, K)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
print("SCALE k=%.4f dims=%s" % (K, tuple(round(v, 3) for v in BODY.dimensions)))

# --------------------------------------------------------------------------- huesos nuevos
bpy.context.view_layer.objects.active = ARM
bpy.ops.object.mode_set(mode="EDIT")
eb = ARM.data.edit_bones

head = eb["head"]
jaw = eb.new("jaw")
jaw.parent = head
jaw.head = head.head + Vector((0.0, 0.02 * K, 0.06 * K))
jaw.tail = jaw.head + Vector((0.0, 0.34 * K, -0.10 * K))
jaw.use_deform = True

for side, sign in (("L", 1.0), ("R", -1.0)):
    src = eb["upperarm." + side]
    upper2 = eb.new("upperarm2." + side)
    upper2.parent = eb["chest"]
    upper2.head = src.head + Vector((sign * 0.06 * K, 0.04 * K, -0.34 * K))
    upper2.tail = upper2.head + Vector((0.0, 0.0, -0.32 * K))
    upper2.use_deform = True

    fore2 = eb.new("forearm2." + side)
    fore2.parent = upper2
    fore2.head = upper2.tail
    fore2.tail = fore2.head + Vector((0.0, 0.0, -0.30 * K))
    fore2.use_deform = True

    hand2 = eb.new("hand2." + side)
    hand2.parent = fore2
    hand2.head = fore2.tail
    hand2.tail = hand2.head + Vector((0.0, 0.0, -0.14 * K))
    hand2.use_deform = True

bpy.ops.object.mode_set(mode="OBJECT")
print("BONES", len(ARM.data.bones), sorted(b.name for b in ARM.data.bones))


# --------------------------------------------------------------------------- materiales
def make_mat(name, rgb, emission=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
        if emission > 0:
            bsdf.inputs["Emission Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
            bsdf.inputs["Emission Strength"].default_value = emission
    mat.diffuse_color = (rgb[0], rgb[1], rgb[2], 1.0)
    return mat


MAT_BODY = make_mat("EB_OrochiBody", (0.13, 0.11, 0.12))
MAT_LAVA = make_mat("EB_OrochiLava", (1.0, 0.28, 0.02), emission=6.0)
MAT_HORN = make_mat("EB_OrochiHorn", (0.22, 0.19, 0.17))


# --------------------------------------------------------------------------- malla nueva
def box_between(name, p0, p1, thickness, mat, group):
    """Caja alineada al eje Z entre dos puntos; queda pesada 100% al grupo."""
    centre = (p0 + p1) * 0.5
    length = max(abs(p1.z - p0.z), thickness)
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = (thickness, thickness, length)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    vg = ob.vertex_groups.new(name=group)
    vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE")
    return ob


bones = ARM.data.bones
pieces = []

for side in ("L", "R"):
    for part, mat in (("upperarm2", MAT_LAVA), ("forearm2", MAT_LAVA), ("hand2", MAT_LAVA)):
        b = bones[part + "." + side]
        pieces.append(box_between("EB_%s_%s" % (part, side), b.head_local, b.tail_local,
                                  0.20 * K, mat, part + "." + side))

# mandíbula
jb = bones["jaw"]
pieces.append(box_between("EB_Jaw", jb.head_local, jb.tail_local, 0.18 * K, MAT_BODY, "jaw"))

# cuernos: dos cajas sobre la cabeza, abiertas hacia afuera
hb = bones["head"]
for side, sign in (("L", 1.0), ("R", -1.0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=hb.head_local + Vector((sign * 0.11 * K, -0.02 * K, (hb.tail_local.z - hb.head_local.z) * 0.92)))
    horn = bpy.context.active_object
    horn.name = "EB_Horn_" + side
    horn.scale = (0.07 * K, 0.07 * K, 0.30 * K)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    horn.rotation_euler = (0.0, sign * -0.45, 0.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    horn.data.materials.append(MAT_HORN)
    vg = horn.vertex_groups.new(name="head")
    vg.add(list(range(len(horn.data.vertices))), 1.0, "REPLACE")
    pieces.append(horn)

bpy.ops.object.select_all(action="DESELECT")
for p in pieces:
    p.select_set(True)
bpy.context.view_layer.objects.active = pieces[0]
bpy.ops.object.join()
extra = bpy.context.active_object

bpy.ops.object.select_all(action="DESELECT")
extra.select_set(True)
BODY.select_set(True)
bpy.context.view_layer.objects.active = BODY
bpy.ops.object.join()
BODY = bpy.context.active_object
print("BODY verts", len(BODY.data.vertices), "groups", len(BODY.vertex_groups),
      "dims", tuple(round(v, 3) for v in BODY.dimensions))


# --------------------------------------------------------------------------- helpers de pose
def frame_of(seconds):
    return 1 + round(seconds * FPS)


def local_offset(world_xyz):
    return (world_xyz[0], world_xyz[2], -world_xyz[1])


def clear_pose():
    for pb in ARM.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)


def apply_pose(pose):
    clear_pose()
    for bone, values in pose.items():
        pb = ARM.pose.bones[bone]
        pb.rotation_euler = (values[0], values[1], values[2])
        if len(values) >= 6:
            pb.location = local_offset((values[3], values[4], values[5]))


def key_all(frame):
    for pb in ARM.pose.bones:
        pb.keyframe_insert("rotation_euler", frame=frame)
        pb.keyframe_insert("location", frame=frame)


def merge(*poses):
    out = {}
    for pose in poses:
        out.update(pose)
    return out


def arms(upper_rx, upper_rz, fore_rx, hand_rx=-0.2):
    """Pose simétrica de los dos pares de brazos; el par inferior barre más abierto."""
    return {
        "upperarm.L": (upper_rx, 0, -upper_rz),
        "upperarm.R": (upper_rx, 0, upper_rz),
        "forearm.L": (fore_rx, 0, 0),
        "forearm.R": (fore_rx, 0, 0),
        "hand.L": (hand_rx, 0, 0),
        "hand.R": (hand_rx, 0, 0),
        "upperarm2.L": (upper_rx * 0.7, 0, -upper_rz * 1.6),
        "upperarm2.R": (upper_rx * 0.7, 0, upper_rz * 1.6),
        "forearm2.L": (fore_rx * 0.8, 0, 0),
        "forearm2.R": (fore_rx * 0.8, 0, 0),
        "hand2.L": (hand_rx, 0, 0),
        "hand2.R": (hand_rx, 0, 0),
    }


IDLE = merge({
    "spine": (-0.06, 0, 0), "chest": (-0.04, 0, 0), "head": (0.08, 0, 0),
    "jaw": (0.0, 0, 0),
    "thigh.L": (0.06, 0, 0.03), "thigh.R": (0.02, 0, -0.03),
    "shin.L": (-0.10, 0, 0), "shin.R": (-0.06, 0, 0),
}, arms(0.06, 0.16, 0.30))

IDLE_BREATH = merge(IDLE, {
    "spine": (-0.02, 0, 0), "chest": (0.01, 0, 0), "head": (0.03, 0, 0),
}, arms(0.02, 0.22, 0.24))

MOVE_A = merge(IDLE, {
    "spine": (-0.14, 0, 0),
    "thigh.L": (0.60, 0, 0), "shin.L": (-0.15, 0, 0), "foot.L": (-0.28, 0, 0),
    "thigh.R": (-0.42, 0, 0), "shin.R": (-0.55, 0, 0), "foot.R": (0.12, 0, 0),
}, arms(0.10, 0.18, 0.34))
MOVE_B = merge(IDLE, {
    "spine": (-0.12, 0, 0),
    "thigh.L": (0.14, 0, 0), "shin.L": (-0.60, 0, 0), "foot.L": (0.06, 0, 0),
    "thigh.R": (-0.08, 0, 0), "shin.R": (-0.20, 0, 0),
}, arms(0.02, 0.18, 0.30))
MOVE_C = merge(IDLE, {
    "spine": (-0.14, 0, 0),
    "thigh.L": (-0.42, 0, 0), "shin.L": (-0.55, 0, 0), "foot.L": (0.12, 0, 0),
    "thigh.R": (0.60, 0, 0), "shin.R": (-0.15, 0, 0), "foot.R": (-0.28, 0, 0),
}, arms(0.06, 0.18, 0.34))
MOVE_D = merge(IDLE, {
    "spine": (-0.12, 0, 0),
    "thigh.L": (-0.08, 0, 0), "shin.L": (-0.20, 0, 0),
    "thigh.R": (0.14, 0, 0), "shin.R": (-0.60, 0, 0), "foot.R": (0.06, 0, 0),
}, arms(0.02, 0.18, 0.30))

SPAWN_KNEEL = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.95),
    "spine": (-0.34, 0, 0), "chest": (-0.22, 0, 0), "head": (-0.12, 0, 0),
    "thigh.L": (1.00, 0, 0), "shin.L": (-1.70, 0, 0), "foot.L": (0.60, 0, 0),
    "thigh.R": (1.45, 0, 0), "shin.R": (-2.10, 0, 0), "foot.R": (0.75, 0, 0),
}, arms(0.40, 0.35, 0.80))

HIT = merge(IDLE, {
    "spine": (0.22, 0, 0), "chest": (0.16, 0, 0), "head": (0.30, 0, 0), "jaw": (0.35, 0, 0),
    "root": (0, 0, 0, 0.0, -0.14, 0.0),
}, arms(-0.28, 0.45, 0.55))

DEATH_STAGGER = merge(IDLE, {
    "spine": (0.38, 0, 0), "chest": (0.24, 0, 0), "head": (0.42, 0, 0), "jaw": (0.40, 0, 0),
    "root": (0, 0, 0, 0.0, -0.18, 0.0),
    "thigh.L": (-0.18, 0, 0), "thigh.R": (-0.12, 0, 0),
}, arms(-0.55, 0.75, 0.60))
DEATH_BUCKLE = merge(IDLE, {
    "root": (0.30, 0, 0, 0.0, -0.20, -0.80),
    "spine": (0.50, 0, 0), "chest": (0.30, 0, 0), "head": (0.30, 0, 0), "jaw": (0.25, 0, 0),
    "thigh.L": (1.20, 0, 0), "shin.L": (-1.75, 0, 0), "foot.L": (0.60, 0, 0),
    "thigh.R": (1.10, 0, 0), "shin.R": (-1.65, 0, 0), "foot.R": (0.55, 0, 0),
}, arms(-0.35, 0.55, 0.45))
DEATH_DOWN = merge(IDLE, {
    "root": (1.50, 0, 0, 0.0, 0.45, -0.15),
    "spine": (0.18, 0, 0), "chest": (0.10, 0, 0), "head": (-0.28, 0, 0), "jaw": (0.15, 0, 0),
    "thigh.L": (0.60, 0, 0), "shin.L": (-0.85, 0, 0), "foot.L": (0.25, 0, 0),
    "thigh.R": (0.50, 0, 0), "shin.R": (-0.75, 0, 0), "foot.R": (0.20, 0, 0),
}, arms(-0.65, 0.95, 0.30))

SLAM_DRAW = merge(IDLE, {
    "spine": (0.12, 0, 0), "chest": (0.06, 0, 0),
}, arms(0.80, 0.55, 1.20, -0.55))
SLAM_OVERHEAD = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, 0.10),
    "spine": (0.26, 0, 0), "chest": (0.18, 0, 0), "head": (0.18, 0, 0), "jaw": (0.30, 0, 0),
}, arms(2.80, 0.25, 0.50, -0.10))
SLAM_DOWN = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.26),
    "spine": (-0.52, 0, 0), "chest": (-0.32, 0, 0), "head": (-0.22, 0, 0),
    "thigh.L": (0.60, 0, 0), "shin.L": (-0.90, 0, 0), "foot.L": (0.38, 0, 0),
    "thigh.R": (0.40, 0, 0), "shin.R": (-0.75, 0, 0), "foot.R": (0.32, 0, 0),
}, arms(1.15, 0.14, 0.25, -0.60))
SLAM_IMPACT = merge(SLAM_DOWN, {
    "root": (0, 0, 0, 0.0, 0.0, -0.34),
    "spine": (-0.66, 0, 0), "chest": (-0.44, 0, 0), "head": (-0.32, 0, 0),
}, arms(0.30, 0.10, 0.08, -0.80))
SLAM_HOLD = merge(SLAM_IMPACT, {
    "spine": (-0.60, 0, 0), "chest": (-0.38, 0, 0),
    "root": (0, 0, 0, 0.0, 0.0, -0.31),
})
SLAM_RISE = merge(IDLE, {
    "spine": (-0.24, 0, 0), "chest": (-0.13, 0, 0),
    "thigh.L": (0.20, 0, 0), "shin.L": (-0.28, 0, 0),
    "thigh.R": (0.12, 0, 0), "shin.R": (-0.22, 0, 0),
}, arms(0.25, 0.25, 0.50))

CHARGE_COIL = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.18),
    "spine": (-0.30, 0, 0), "chest": (-0.20, 0, 0), "head": (-0.14, 0, 0),
    "thigh.L": (0.34, 0, 0), "shin.L": (-0.50, 0, 0), "foot.L": (0.22, 0, 0),
    "thigh.R": (0.18, 0, 0), "shin.R": (-0.38, 0, 0), "foot.R": (0.18, 0, 0),
}, arms(-0.45, 0.70, 1.30, -0.50))
CHARGE_READY = merge(CHARGE_COIL, {
    "root": (0, 0, 0, 0.0, 0.0, -0.28),
    "spine": (-0.46, 0, 0), "chest": (-0.28, 0, 0), "head": (-0.04, 0, 0),
    "thigh.L": (0.50, 0, 0), "shin.L": (-0.78, 0, 0),
    "thigh.R": (0.24, 0, 0), "shin.R": (-0.55, 0, 0),
}, arms(-0.60, 0.85, 1.45, -0.55))

RUN_A = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.14),
    "spine": (-0.58, 0, 0), "chest": (-0.32, 0, 0), "head": (0.28, 0, 0),
    "thigh.L": (0.90, 0, 0), "shin.L": (-0.32, 0, 0), "foot.L": (-0.16, 0, 0),
    "thigh.R": (-0.58, 0, 0), "shin.R": (-1.00, 0, 0), "foot.R": (0.32, 0, 0),
}, arms(-0.70, 0.60, 1.20, -0.55))
RUN_B = merge(RUN_A, {
    "thigh.L": (-0.58, 0, 0), "shin.L": (-1.00, 0, 0), "foot.L": (0.32, 0, 0),
    "thigh.R": (0.90, 0, 0), "shin.R": (-0.32, 0, 0), "foot.R": (-0.16, 0, 0),
    "root": (0, 0, 0, 0.0, 0.0, -0.04),
}, arms(-0.52, 0.60, 1.30, -0.55))

RECOVER_START = merge(RUN_A, {
    "root": (0, 0, 0, 0.0, 0.0, -0.18),
    "spine": (-0.32, 0, 0), "chest": (-0.16, 0, 0), "head": (0.06, 0, 0),
    "thigh.L": (0.40, 0, 0), "shin.L": (-0.60, 0, 0),
    "thigh.R": (-0.22, 0, 0), "shin.R": (-0.50, 0, 0),
})
RECOVER_STAGGER = merge(IDLE, {
    "root": (0, 0, 0, 0.0, -0.12, -0.18),
    "spine": (0.20, 0, 0), "chest": (0.14, 0, 0), "head": (0.28, 0, 0), "jaw": (0.25, 0, 0),
    "thigh.L": (0.28, 0, 0), "shin.L": (-0.50, 0, 0),
    "thigh.R": (0.12, 0, 0), "shin.R": (-0.34, 0, 0),
}, arms(-0.22, 0.55, 0.45))

ROAR_UP = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, 0.10),
    "spine": (0.24, 0, 0), "chest": (0.20, 0, 0), "head": (0.42, 0, 0), "jaw": (0.55, 0, 0),
    "thigh.L": (0.12, 0, 0), "thigh.R": (-0.10, 0, 0),
}, arms(0.45, 1.25, 0.55))
ROAR_PEAK = merge(ROAR_UP, {
    "spine": (0.32, 0, 0), "head": (0.55, 0, 0), "jaw": (0.75, 0, 0),
}, arms(0.55, 1.45, 0.60))

CLIPS = [
    ("Spawn", 0.40, [(0.00, SPAWN_KNEEL), (0.22, merge(IDLE, {
        "root": (0, 0, 0, 0.0, 0.0, -0.30),
        "spine": (-0.14, 0, 0),
        "thigh.L": (0.40, 0, 0), "shin.L": (-0.65, 0, 0),
        "thigh.R": (0.50, 0, 0), "shin.R": (-0.80, 0, 0),
    })), (0.40, IDLE)]),
    ("Idle", 1.80, [(0.00, IDLE), (0.90, IDLE_BREATH), (1.80, IDLE)]),
    ("Move", 0.75, [(0.00, MOVE_A), (0.1875, MOVE_B), (0.375, MOVE_C),
                    (0.5625, MOVE_D), (0.75, MOVE_A)]),
    ("Hit", 0.25, [(0.00, IDLE), (0.07, HIT), (0.25, IDLE)]),
    ("Death", 2.20, [(0.00, IDLE), (0.35, DEATH_STAGGER), (1.05, DEATH_BUCKLE),
                     (1.65, DEATH_DOWN), (2.20, DEATH_DOWN)]),
    ("Slam", 2.80, [(0.00, IDLE), (0.22, SLAM_DRAW), (0.55, SLAM_OVERHEAD),
                    (0.70, SLAM_DOWN), (0.80, SLAM_IMPACT), (1.05, SLAM_HOLD),
                    (1.70, SLAM_RISE), (2.80, IDLE)]),
    ("Charge", 0.80, [(0.00, IDLE), (0.30, CHARGE_COIL), (0.80, CHARGE_READY)]),
    ("ChargeRun", 0.50, [(0.00, RUN_A), (0.25, RUN_B), (0.50, RUN_A)]),
    ("ChargeRecover", 1.80, [(0.00, RECOVER_START), (0.35, RECOVER_STAGGER),
                             (0.90, merge(IDLE, {"spine": (-0.12, 0, 0)})),
                             (1.80, IDLE)]),
    ("Roar", 1.50, [(0.00, IDLE), (0.35, ROAR_UP), (0.85, ROAR_PEAK), (1.50, IDLE)]),
]

ARM.animation_data_create()
for name, duration, keys in CLIPS:
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    ARM.animation_data.action = action
    clear_pose()
    for seconds, pose in keys:
        apply_pose(pose)
        key_all(frame_of(seconds))
    print("CLIP %-14s 1..%d" % (name, frame_of(duration)))

ARM.animation_data.action = None
clear_pose()
bpy.context.scene.frame_set(1)
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = frame_of(2.80)

bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
print("SAVED", OUT_BLEND)
