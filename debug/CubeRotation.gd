extends MeshInstance3D

func _physics_process(delta):
    self.rotate_y(delta)
