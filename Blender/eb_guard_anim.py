"""Generador de animaciones del Guardia (boss A).

Se ejecuta DENTRO de Blender, sobre `EB_Guard.blend` ya construido:
  exec(open("Blender/eb_guard_anim.py").read())

Contrato (ver .atl/sdd/boss-animation-roster/spec.md):
  keys = seg * 60 + 1 · se keyean TODOS los huesos en TODOS los keyframes
  (rotación + locación), porque un canal sin keyear hereda el clip anterior.

Mapeo de ejes del rig (verificado): la X local de cada hueso es la X del mundo.
  · cadena spine/chest/head (hueso apunta +Z): rx NEGATIVO = inclinarse adelante
  · brazos y piernas (hueso apunta -Z):     rx POSITIVO = ir hacia adelante
  · shin: rx NEGATIVO = flexionar la rodilla (talón al glúteo)
  · brazo rz: .L negativo / .R positivo = abrir hacia afuera
La locación de root/hips va en espacio del hueso: local = (wx, wz, -wy).
"""

import bpy
from mathutils import Vector

FPS = 60
ARM = bpy.data.objects["EB_Rig_Guard"]
BODY = bpy.data.objects["EB_Body"]
WEAPON_GROUP = "weapon"


# --------------------------------------------------------------------------- helpers
def frame_of(seconds):
    return 1 + round(seconds * FPS)


def local_offset(world_xyz):
    """World (x, y, z) -> bone-local for an up-pointing bone (root/hips)."""
    wx, wy, wz = world_xyz
    return (wx, wz, -wy)


def clear_pose():
    for pb in ARM.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)


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


# --------------------------------------------------------------------------- poses
IDLE = {
    "spine": (-0.05, 0, 0),
    "chest": (-0.03, 0, 0),
    "head": (0.06, 0, 0),
    "upperarm.L": (0.05, 0, -0.12),
    "upperarm.R": (0.05, 0, 0.12),
    "forearm.L": (0.22, 0, 0),
    "forearm.R": (0.28, 0, 0),
    "hand.R": (-0.25, 0, 0),
}

IDLE_BREATH = merge(IDLE, {
    "spine": (-0.02, 0, 0),
    "chest": (0.01, 0, 0),
    "head": (0.02, 0, 0),
    "upperarm.L": (0.02, 0, -0.16),
    "upperarm.R": (0.02, 0, 0.16),
})

# --- move: ciclo de caminar lento (0.5 s) ---
MOVE_A = merge(IDLE, {
    "thigh.L": (0.55, 0, 0), "shin.L": (-0.12, 0, 0), "foot.L": (-0.25, 0, 0),
    "thigh.R": (-0.38, 0, 0), "shin.R": (-0.50, 0, 0), "foot.R": (0.10, 0, 0),
    "upperarm.L": (-0.22, 0, -0.12), "upperarm.R": (0.28, 0, 0.12),
    "spine": (-0.10, 0, 0),
})
MOVE_B = merge(IDLE, {
    "thigh.L": (0.12, 0, 0), "shin.L": (-0.55, 0, 0), "foot.L": (0.05, 0, 0),
    "thigh.R": (-0.06, 0, 0), "shin.R": (-0.18, 0, 0), "foot.R": (0.0, 0, 0),
    "upperarm.L": (-0.05, 0, -0.12), "upperarm.R": (0.10, 0, 0.12),
    "spine": (-0.08, 0, 0),
})
MOVE_C = merge(IDLE, {
    "thigh.L": (-0.38, 0, 0), "shin.L": (-0.50, 0, 0), "foot.L": (0.10, 0, 0),
    "thigh.R": (0.55, 0, 0), "shin.R": (-0.12, 0, 0), "foot.R": (-0.25, 0, 0),
    "upperarm.L": (0.28, 0, -0.12), "upperarm.R": (-0.10, 0, 0.12),
    "spine": (-0.10, 0, 0),
})
MOVE_D = merge(IDLE, {
    "thigh.L": (-0.06, 0, 0), "shin.L": (-0.18, 0, 0), "foot.L": (0.0, 0, 0),
    "thigh.R": (0.12, 0, 0), "shin.R": (-0.55, 0, 0), "foot.R": (0.05, 0, 0),
    "upperarm.L": (0.10, 0, -0.12), "upperarm.R": (0.02, 0, 0.12),
    "spine": (-0.08, 0, 0),
})

# --- spawn: se levanta de una rodilla (0.4 s) ---
SPAWN_KNEEL = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.52),
    "spine": (-0.30, 0, 0), "chest": (-0.20, 0, 0), "head": (-0.10, 0, 0),
    "thigh.L": (0.90, 0, 0), "shin.L": (-1.55, 0, 0), "foot.L": (0.55, 0, 0),
    "thigh.R": (1.35, 0, 0), "shin.R": (-2.00, 0, 0), "foot.R": (0.70, 0, 0),
    "upperarm.L": (0.35, 0, -0.20), "upperarm.R": (0.30, 0, 0.20),
    "forearm.L": (0.55, 0, 0), "forearm.R": (0.50, 0, 0),
})

# --- hit: retroceso corto (0.25 s) ---
HIT = merge(IDLE, {
    "spine": (0.24, 0, 0), "chest": (0.18, 0, 0), "head": (0.34, 0, 0),
    "upperarm.L": (-0.30, 0, -0.28), "upperarm.R": (-0.26, 0, 0.28),
    "forearm.L": (0.45, 0, 0), "forearm.R": (0.50, 0, 0),
    "root": (0, 0, 0, 0.0, -0.10, 0.0),
})

# --- death: se derrumba de espaldas (1.5 s) ---
DEATH_STAGGER = merge(IDLE, {
    "spine": (0.35, 0, 0), "chest": (0.22, 0, 0), "head": (0.40, 0, 0),
    "upperarm.L": (-0.45, 0, -0.55), "upperarm.R": (-0.40, 0, 0.55),
    "thigh.L": (-0.15, 0, 0), "thigh.R": (-0.10, 0, 0),
    "root": (0, 0, 0, 0.0, -0.12, 0.0),
})
DEATH_BUCKLE = merge(IDLE, {
    "root": (0.25, 0, 0, 0.0, -0.15, -0.45),
    "spine": (0.45, 0, 0), "chest": (0.25, 0, 0), "head": (0.30, 0, 0),
    "thigh.L": (1.10, 0, 0), "shin.L": (-1.60, 0, 0), "foot.L": (0.55, 0, 0),
    "thigh.R": (1.00, 0, 0), "shin.R": (-1.50, 0, 0), "foot.R": (0.50, 0, 0),
    "upperarm.L": (-0.30, 0, -0.35), "upperarm.R": (-0.25, 0, 0.35),
})
DEATH_DOWN = merge(IDLE, {
    "root": (1.45, 0, 0, 0.0, 0.35, -0.10),
    "spine": (0.15, 0, 0), "chest": (0.10, 0, 0), "head": (-0.25, 0, 0),
    "thigh.L": (0.55, 0, 0), "shin.L": (-0.75, 0, 0), "foot.L": (0.20, 0, 0),
    "thigh.R": (0.45, 0, 0), "shin.R": (-0.65, 0, 0), "foot.R": (0.15, 0, 0),
    "upperarm.L": (-0.55, 0, -0.75), "upperarm.R": (-0.50, 0, 0.70),
    "forearm.L": (0.30, 0, 0), "forearm.R": (0.35, 0, 0),
})

# --- slam: el golpe cae exactamente en slamTell = 0.80 (2.8 s) ---
SLAM_DRAW = merge(IDLE, {
    "spine": (0.10, 0, 0), "chest": (0.05, 0, 0), "head": (0.05, 0, 0),
    "upperarm.R": (0.75, 0, 0.30), "forearm.R": (1.15, 0, 0), "hand.R": (-0.60, 0, 0),
    "upperarm.L": (0.30, 0, -0.30), "forearm.L": (0.85, 0, 0),
    "thigh.L": (0.20, 0, 0), "shin.L": (-0.30, 0, 0),
    "thigh.R": (-0.10, 0, 0), "shin.R": (-0.20, 0, 0),
})
SLAM_OVERHEAD = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, 0.06),
    "spine": (0.22, 0, 0), "chest": (0.15, 0, 0), "head": (0.15, 0, 0),
    "upperarm.R": (2.75, 0, 0.12), "forearm.R": (0.45, 0, 0), "hand.R": (-0.10, 0, 0),
    "upperarm.L": (2.45, 0, -0.12), "forearm.L": (0.55, 0, 0),
    "thigh.L": (0.10, 0, 0), "thigh.R": (-0.05, 0, 0),
})
SLAM_DOWN = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.16),
    "spine": (-0.50, 0, 0), "chest": (-0.30, 0, 0), "head": (-0.20, 0, 0),
    "upperarm.R": (1.10, 0, 0.08), "forearm.R": (0.20, 0, 0), "hand.R": (-0.55, 0, 0),
    "upperarm.L": (0.95, 0, -0.08), "forearm.L": (0.30, 0, 0),
    "thigh.L": (0.55, 0, 0), "shin.L": (-0.85, 0, 0), "foot.L": (0.35, 0, 0),
    "thigh.R": (0.35, 0, 0), "shin.R": (-0.70, 0, 0), "foot.R": (0.30, 0, 0),
})
SLAM_IMPACT = merge(SLAM_DOWN, {
    "spine": (-0.62, 0, 0), "chest": (-0.40, 0, 0), "head": (-0.30, 0, 0),
    "upperarm.R": (0.35, 0, 0.05), "forearm.R": (0.05, 0, 0), "hand.R": (-0.75, 0, 0),
    "upperarm.L": (0.25, 0, -0.05), "forearm.L": (0.10, 0, 0),
    "root": (0, 0, 0, 0.0, 0.0, -0.22),
})
SLAM_HOLD = merge(SLAM_IMPACT, {
    "spine": (-0.58, 0, 0), "chest": (-0.36, 0, 0),
    "root": (0, 0, 0, 0.0, 0.0, -0.20),
})
SLAM_RISE = merge(IDLE, {
    "spine": (-0.22, 0, 0), "chest": (-0.12, 0, 0),
    "upperarm.R": (0.20, 0, 0.20), "forearm.R": (0.45, 0, 0), "hand.R": (-0.40, 0, 0),
    "upperarm.L": (0.12, 0, -0.18), "forearm.L": (0.40, 0, 0),
    "thigh.L": (0.18, 0, 0), "shin.L": (-0.25, 0, 0),
    "thigh.R": (0.10, 0, 0), "shin.R": (-0.20, 0, 0),
})

# --- charge: espera telegrafiada (0.8 s) ---
CHARGE_COIL = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.10),
    "spine": (-0.28, 0, 0), "chest": (-0.18, 0, 0), "head": (-0.12, 0, 0),
    "upperarm.R": (-0.35, 0, 0.35), "forearm.R": (1.30, 0, 0), "hand.R": (-0.50, 0, 0),
    "upperarm.L": (-0.25, 0, -0.30), "forearm.L": (1.10, 0, 0),
    "thigh.L": (0.30, 0, 0), "shin.L": (-0.45, 0, 0), "foot.L": (0.20, 0, 0),
    "thigh.R": (0.15, 0, 0), "shin.R": (-0.35, 0, 0), "foot.R": (0.15, 0, 0),
})
CHARGE_READY = merge(CHARGE_COIL, {
    "root": (0, 0, 0, 0.0, 0.0, -0.16),
    "spine": (-0.42, 0, 0), "chest": (-0.25, 0, 0), "head": (-0.05, 0, 0),
    "upperarm.R": (-0.50, 0, 0.30), "forearm.R": (1.45, 0, 0),
    "thigh.L": (0.45, 0, 0), "shin.L": (-0.70, 0, 0),
    "thigh.R": (0.20, 0, 0), "shin.R": (-0.50, 0, 0),
})

# --- charge run: ciclo de embestida (0.5 s, loop) ---
RUN_A = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, -0.08),
    "spine": (-0.55, 0, 0), "chest": (-0.30, 0, 0), "head": (0.25, 0, 0),
    "upperarm.R": (-0.60, 0, 0.25), "forearm.R": (1.20, 0, 0), "hand.R": (-0.55, 0, 0),
    "upperarm.L": (-0.45, 0, -0.25), "forearm.L": (0.95, 0, 0),
    "thigh.L": (0.85, 0, 0), "shin.L": (-0.30, 0, 0), "foot.L": (-0.15, 0, 0),
    "thigh.R": (-0.55, 0, 0), "shin.R": (-0.95, 0, 0), "foot.R": (0.30, 0, 0),
})
RUN_B = merge(RUN_A, {
    "thigh.L": (-0.55, 0, 0), "shin.L": (-0.95, 0, 0), "foot.L": (0.30, 0, 0),
    "thigh.R": (0.85, 0, 0), "shin.R": (-0.30, 0, 0), "foot.R": (-0.15, 0, 0),
    "upperarm.R": (-0.40, 0, 0.25), "upperarm.L": (-0.60, 0, -0.25),
    "root": (0, 0, 0, 0.0, 0.0, -0.02),
})

# --- charge recover: se recompone del pisotón (2.0 s) ---
RECOVER_START = merge(RUN_A, {
    "root": (0, 0, 0, 0.0, 0.0, -0.10),
    "spine": (-0.30, 0, 0), "chest": (-0.15, 0, 0), "head": (0.05, 0, 0),
    "thigh.L": (0.35, 0, 0), "shin.L": (-0.55, 0, 0),
    "thigh.R": (-0.20, 0, 0), "shin.R": (-0.45, 0, 0),
})
RECOVER_STAGGER = merge(IDLE, {
    "root": (0, 0, 0, 0.0, -0.08, -0.12),
    "spine": (0.18, 0, 0), "chest": (0.12, 0, 0), "head": (0.25, 0, 0),
    "upperarm.L": (-0.20, 0, -0.35), "upperarm.R": (-0.18, 0, 0.35),
    "forearm.L": (0.40, 0, 0), "forearm.R": (0.45, 0, 0),
    "thigh.L": (0.25, 0, 0), "shin.L": (-0.45, 0, 0),
    "thigh.R": (0.10, 0, 0), "shin.R": (-0.30, 0, 0),
})

# --- roar: se yergue y ruge (1.2 s) ---
ROAR_UP = merge(IDLE, {
    "root": (0, 0, 0, 0.0, 0.0, 0.05),
    "spine": (0.20, 0, 0), "chest": (0.18, 0, 0), "head": (0.38, 0, 0),
    "upperarm.L": (0.35, 0, -0.95), "upperarm.R": (0.30, 0, 0.95),
    "forearm.L": (0.55, 0, 0), "forearm.R": (0.60, 0, 0),
    "thigh.L": (0.10, 0, 0), "thigh.R": (-0.08, 0, 0),
})
ROAR_PEAK = merge(ROAR_UP, {
    "spine": (0.28, 0, 0), "head": (0.50, 0, 0),
    "upperarm.L": (0.45, 0, -1.15), "upperarm.R": (0.40, 0, 1.15),
})


# --------------------------------------------------------------------------- clips
# (nombre, segundos totales, [(segundo, pose), ...])
CLIPS = [
    ("Spawn", 0.40, [(0.00, SPAWN_KNEEL), (0.22, merge(IDLE, {
        "root": (0, 0, 0, 0.0, 0.0, -0.16),
        "spine": (-0.12, 0, 0),
        "thigh.L": (0.35, 0, 0), "shin.L": (-0.60, 0, 0),
        "thigh.R": (0.45, 0, 0), "shin.R": (-0.75, 0, 0),
    })), (0.40, IDLE)]),
    ("Idle", 1.50, [(0.00, IDLE), (0.75, IDLE_BREATH), (1.50, IDLE)]),
    ("Move", 0.50, [(0.00, MOVE_A), (0.125, MOVE_B), (0.25, MOVE_C),
                    (0.375, MOVE_D), (0.50, MOVE_A)]),
    ("Hit", 0.25, [(0.00, IDLE), (0.07, HIT), (0.25, IDLE)]),
    ("Death", 1.50, [(0.00, IDLE), (0.28, DEATH_STAGGER), (0.72, DEATH_BUCKLE),
                     (1.10, DEATH_DOWN), (1.50, DEATH_DOWN)]),
    ("Slam", 2.80, [(0.00, IDLE), (0.22, SLAM_DRAW), (0.55, SLAM_OVERHEAD),
                    (0.70, SLAM_DOWN), (0.80, SLAM_IMPACT), (1.05, SLAM_HOLD),
                    (1.70, SLAM_RISE), (2.80, IDLE)]),
    ("Charge", 0.80, [(0.00, IDLE), (0.30, CHARGE_COIL), (0.80, CHARGE_READY)]),
    ("ChargeRun", 0.50, [(0.00, RUN_A), (0.25, RUN_B), (0.50, RUN_A)]),
    ("ChargeRecover", 2.00, [(0.00, RECOVER_START), (0.35, RECOVER_STAGGER),
                             (0.90, merge(IDLE, {"spine": (-0.10, 0, 0)})),
                             (2.00, IDLE)]),
    ("Roar", 1.20, [(0.00, IDLE), (0.30, ROAR_UP), (0.70, ROAR_PEAK), (1.20, IDLE)]),
]


# --------------------------------------------------------------------------- build
def build():
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)

    ARM.animation_data_create()
    summary = []

    for name, duration, keys in CLIPS:
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        ARM.animation_data.action = action
        clear_pose()

        for seconds, pose in keys:
            apply_pose(pose)
            key_all(frame_of(seconds))

        last = frame_of(duration)
        # Los clips de ataque y muerte son one-shot; los de locomoción deben loopear.
        if name in ("Idle", "Move", "ChargeRun"):
            for fc in action.layers[0].strips[0].channelbag(action.slots[0]).fcurves:
                for kp in fc.keyframe_points:
                    kp.interpolation = "BEZIER"

        summary.append((name, action.frame_range[0], action.frame_range[1],
                        len(action.slots)))

    ARM.animation_data.action = None
    clear_pose()
    bpy.context.scene.frame_set(1)
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = frame_of(2.80)

    for row in summary:
        print("CLIP %-14s %6.1f..%-6.1f slots=%d" % row)


build()
print("TOTAL ACTIONS", len(bpy.data.actions), [a.name for a in bpy.data.actions])
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print("SAVED", bpy.data.filepath)
