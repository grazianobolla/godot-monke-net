@tool
extends EditorPlugin

func _enter_tree():
	add_autoload_singleton("MonkeNet", "res://addons/monke-net/scenes/MonkeNet.tscn")
	pass


func _exit_tree():
	remove_autoload_singleton("MonkeNet")
	pass
