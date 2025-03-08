extends Button

const SAVED_LAYOUT_PATH := "user://layout.tres"

@export var _container: DockableContainer

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


func _on_save_pressed() -> void:
	if ResourceSaver.save(_container.layout, SAVED_LAYOUT_PATH) != OK:
		print("ERROR")
