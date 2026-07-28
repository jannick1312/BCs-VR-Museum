extends SceneTree


func _init() -> void:
	DirAccess.make_dir_recursive_absolute("res://native")
	var directory := DirAccess.open("res://assets")
	if directory == null:
		push_error("Cannot open res://assets")
		quit(1)
		return

	var converted := 0
	directory.list_dir_begin()
	var name := directory.get_next()
	while name != "":
		if not directory.current_is_dir() and name.get_extension().to_lower() == "glb":
			var source := "res://assets/".path_join(name)
			var destination := "res://native/".path_join(
				name.get_basename() + ".scn"
			)
			var packed := ResourceLoader.load(source, "PackedScene") as PackedScene
			if packed == null:
				push_error("Could not load imported scene: %s" % source)
				quit(1)
				return
			var error := ResourceSaver.save(packed, destination)
			if error != OK:
				push_error("Could not save native scene: %s (%d)" % [destination, error])
				quit(1)
				return
			converted += 1
		name = directory.get_next()
	directory.list_dir_end()
	print("Prepared %d native PackedScene resource(s)." % converted)
	quit()
