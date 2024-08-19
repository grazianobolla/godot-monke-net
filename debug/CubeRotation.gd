extends MeshInstance3D

func _process(delta):
	self.rotate_y(delta * 4)
