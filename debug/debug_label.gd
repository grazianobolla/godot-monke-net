extends Label

func _process(_delta):
	var players = get_node("/root/Main/CharacterArray").get_children()

	self.text = ""
	for player in players:
		self.text += player.name + " " + str(player.position.snapped(Vector3.ONE*0.1)) + "\n"
