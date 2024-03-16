extends MeshInstance3D

var dir = 1
func _process(delta):
    translate(Vector3.LEFT * dir * delta * 5)

    if position.x <= (-5) and dir == 1:
        dir = -1
    elif position.x >= 5 and dir == - 1:
        dir = 1