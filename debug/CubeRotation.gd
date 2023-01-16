extends MeshInstance3D

func _process(delta):
    #self.rotate_y(delta)
    self.translate(Vector3.LEFT * sin(Time.get_ticks_msec() / 1000.0) * delta * 4)
