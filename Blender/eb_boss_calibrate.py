"""Calibración de piso por keyframe para los bosses.

Se ejecuta DENTRO de Blender, sobre el .blend del arquetipo ya animado:
  exec(open("Blender/eb_boss_calibrate.py").read())

Regla: en cada keyframe, el punto más bajo de la MALLA DEL CUERPO queda en Z = 0
(±2 mm). Se excluyen los vértices del grupo `weapon`: el arma puede apoyarse o
pasar por debajo del piso (un hachazo termina en el suelo), y si se incluyera, la
calibración levantaría al personaje cada vez que el arma baja.

El ajuste se aplica sobre la locación del hueso `root`. Su eje local Y es el Z del
mundo, así que `location[1]` es la altura.
"""

import bpy

ARM = bpy.data.objects.get("EB_Rig_Guard") or bpy.data.objects.get("EB_Rig_Orochi")
BODY = bpy.data.objects["EB_Body"]
TOLERANCE = 0.002

weapon_group = BODY.vertex_groups.get("weapon")
weapon_index = weapon_group.index if weapon_group else -1
weapon_verts = set()
if weapon_index >= 0:
    for v in BODY.data.vertices:
        if any(g.group == weapon_index and g.weight > 0.5 for g in v.groups):
            weapon_verts.add(v.index)

body_verts = [i for i in range(len(BODY.data.vertices)) if i not in weapon_verts]
print("RIG", ARM.name, "body verts", len(body_verts), "weapon verts", len(weapon_verts))


def fcurves_of(action):
    for layer in action.layers:
        for strip in layer.strips:
            for slot in action.slots:
                bag = strip.channelbag(slot)
                if bag:
                    for fc in bag.fcurves:
                        yield fc


def key_frames(action):
    frames = set()
    for fc in fcurves_of(action):
        for kp in fc.keyframe_points:
            frames.add(round(kp.co[0]))
    return sorted(frames)


def min_body_z():
    dg = bpy.context.evaluated_depsgraph_get()
    ev = BODY.evaluated_get(dg)
    me = ev.data
    mw = ev.matrix_world
    return min((mw @ me.vertices[i].co).z for i in body_verts)


def calibrate(action):
    ARM.animation_data.action = action
    root = ARM.pose.bones["root"]
    frames = key_frames(action)
    worst = 0.0

    for frame in frames:
        bpy.context.scene.frame_set(frame)
        for _ in range(3):  # itera: mover el root no es perfectamente lineal por el skinning
            z = min_body_z()
            if abs(z) <= TOLERANCE:
                break
            root.location[1] -= z
            root.keyframe_insert("location", frame=frame)
            bpy.context.scene.frame_set(frame)

        z = min_body_z()
        worst = max(worst, abs(z))

    return len(frames), worst


print("--- calibración ---")
for action in sorted(bpy.data.actions, key=lambda a: a.name):
    count, worst = calibrate(action)
    status = "OK" if worst <= TOLERANCE else "FUERA DE TOLERANCIA"
    print("%-14s keyframes=%-4d peor|minZ|=%.4f  %s" % (action.name, count, worst, status))

ARM.animation_data.action = None
for pb in ARM.pose.bones:
    pb.location = (0.0, 0.0, 0.0)
    pb.rotation_euler = (0.0, 0.0, 0.0)
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print("SAVED", bpy.data.filepath)
