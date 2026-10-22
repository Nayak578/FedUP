"""Clean the Fed Up house (ver2.blend copy) and export ENV_Apartment_v01.fbx.
Never saves over the source .blend. Prints a before/after log."""
import bpy, bmesh, sys
from mathutils import Vector

out_path = sys.argv[sys.argv.index("--") + 1]
for _ob in bpy.data.objects:
    if _ob.mode != "OBJECT":
        with bpy.context.temp_override(active_object=_ob, object=_ob, selected_objects=[_ob]):
            bpy.ops.object.mode_set(mode="OBJECT")
        print("CLEANLOG left edit mode on", _ob.name)
log = []
def L(*a): log.append(" ".join(str(x) for x in a))

def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons) if ob.type == "MESH" else 0

meshes = [o for o in bpy.data.objects if o.type == "MESH"]
L("BEFORE meshes", len(meshes), "tris", sum(tris(o) for o in meshes))

# House centre from the shell
main = bpy.data.objects["Main"]
mw = main.matrix_world
cs = [mw @ Vector(c) for c in main.bound_box]
centre = sum(cs, Vector()) / 8.0

# -1. Unparent every mesh while keeping its world placement (parents are removed later)
_mws = {o.name: o.matrix_world.copy() for o in bpy.data.objects}
for o in bpy.data.objects:
    if o.type == "MESH" and o.parent is not None:
        o.parent = None
        o.matrix_world = _mws[o.name]
        L("unparented", o.name)

# 0. Realise collection instances (light switches, wall box) at their placed positions,
#    then drop the template copies that sit at the world origin.
from mathutils import Matrix
templates = set()
for emp in [o for o in bpy.data.objects if o.type == "EMPTY" and o.instance_type == "COLLECTION" and o.instance_collection]:
    if emp.hide_get() if emp.name in bpy.context.view_layer.objects else True:
        continue
    coll = emp.instance_collection
    off = Matrix.Translation(-coll.instance_offset)
    for src in coll.all_objects:
        if src.type != "MESH":
            continue
        templates.add(src.name)
        cp = src.copy(); cp.data = src.data.copy(); cp.parent = None
        bpy.context.scene.collection.objects.link(cp)
        cp.matrix_world = emp.matrix_world @ off @ src.matrix_world
        cp.name = f"{emp.name}_{src.name}"
        L("instanced", cp.name)
for n in templates:
    ob = bpy.data.objects.get(n)
    if ob:
        bpy.data.objects.remove(ob, do_unlink=True)
L("removed origin templates", sorted(templates))

# 1. Remove everything that is not a visible mesh, plus far-away strays
removed = []
for ob in list(bpy.data.objects):
    far = False
    if ob.type == "MESH":
        bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
        far = any((p - centre).length > 60 for p in bb)
    hidden = ob.hide_get() or ob.hide_viewport or ob.hide_render or ob.name not in bpy.context.view_layer.objects
    if ob.type != "MESH" or hidden or far:
        removed.append(f"{ob.name}({ob.type}{',hidden' if hidden else ''}{',far' if far else ''})")
        bpy.data.objects.remove(ob, do_unlink=True)
L("REMOVED", len(removed), ":", ", ".join(removed))

# 2. Apply modifiers + transforms on what is left (data API, no operators)
dg = bpy.context.evaluated_depsgraph_get()
for ob in [o for o in bpy.data.objects if o.type == "MESH"]:
    ev = ob.evaluated_get(dg)
    new = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
    # bake rotation+scale (and parent chain) into vertices, keep world location on the object
    mw = ob.matrix_world.copy()
    loc, rot, sca = mw.decompose()
    from mathutils import Matrix
    new.transform(Matrix.LocRotScale(None, rot, sca))
    ob.modifiers.clear()
    ob.parent = None
    ob.data = new
    ob.matrix_world = Matrix.Translation(loc)
# 3. Per-mesh cleanup
for ob in [o for o in bpy.data.objects if o.type == "MESH"]:
    before = tris(ob)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    r = bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0001)
    loose = [v for v in bm.verts if not v.link_edges]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    # small closed props with inconsistent normals: recalc outward (never the shell)
    is_shell = ob.name in ("Main", "Cube.055", "Cube.050", "Plane.015", "Plane.032", "Plane.034", "Cube.031", "Stairs__2_.001")
    if not is_shell and all(e.is_manifold for e in bm.edges) and len(bm.faces) > 0:
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(ob.data); bm.free()
    # thin pipes/posts: 1404 tris each is wasteful
    if ob.name.startswith("Cylinder") and before >= 1000:
        d = ob.modifiers.new("dec", "DECIMATE"); d.ratio = 0.1
        dg2 = bpy.context.evaluated_depsgraph_get()
        nm = bpy.data.meshes.new_from_object(ob.evaluated_get(dg2), depsgraph=dg2)
        ob.modifiers.clear(); ob.data = nm
    if tris(ob) != before:
        L("cleaned", ob.name, before, "->", tris(ob))

# 3b. Door cuts: open doorways the designer asked for (Blender coordinates; Unity x = -X, Unity z = -Y, Unity y = Z).
#     West porch front door (designer ruling 22 Oct): the north 1.1 m of the living-room window, floor to sill.
DOOR_CUTS = [
    {"name": "west porch front door", "x": (47.45, 48.45), "y": (2.75, 3.90), "z": (4.70, 6.08)},
]
from mathutils import Vector as _V
for cut in DOOR_CUTS:
    removed = 0
    for name in ("Main", "Cube.055"):
        ob = bpy.data.objects.get(name)
        if not ob:
            continue
        inv = ob.matrix_world.inverted()
        bm = bmesh.new(); bm.from_mesh(ob.data)
        for co, no in (((0, cut["y"][0], 0), (0, 1, 0)), ((0, cut["y"][1], 0), (0, 1, 0)), ((0, 0, cut["z"][1]), (0, 0, 1))):
            p = inv @ _V(co); n = (inv.to_3x3().transposed().inverted() @ _V(no)).normalized()
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=p, plane_no=n)
        mw = ob.matrix_world
        doomed = []
        for f in bm.faces:
            c = mw @ f.calc_center_median()
            if cut["x"][0] <= c.x <= cut["x"][1] and cut["y"][0] <= c.y <= cut["y"][1] and cut["z"][0] <= c.z <= cut["z"][1]:
                doomed.append(f)
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        removed += len(doomed)
        bm.to_mesh(ob.data); bm.free()
    L("door cut", cut["name"], "removed faces:", removed)

# 4. Double-side the shell's one-sided surfaces (ceilings/roof visible from both sides)
for name in ("Main", "Cube.055"):
    ob = bpy.data.objects.get(name)
    if not ob:
        continue
    bm = bmesh.new(); bm.from_mesh(ob.data)
    dup = bmesh.ops.duplicate(bm, geom=bm.faces[:])
    new_faces = [g for g in dup["geom"] if isinstance(g, bmesh.types.BMFace)]
    bmesh.ops.reverse_faces(bm, faces=new_faces)
    bm.to_mesh(ob.data); bm.free()
    L("double-sided open faces on", name, ":", len(new_faces), "faces")

meshes = [o for o in bpy.data.objects if o.type == "MESH"]
L("AFTER meshes", len(meshes), "tris", sum(tris(o) for o in meshes))

bpy.ops.export_scene.fbx(filepath=out_path, use_selection=False, object_types={"MESH"},
                         apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", use_mesh_modifiers=True,
                         mesh_smooth_type="FACE", use_tspace=True, bake_anim=False,
                         path_mode="STRIP", add_leaf_bones=False)
L("EXPORTED", out_path)
print("\n".join("CLEANLOG " + l for l in log))
