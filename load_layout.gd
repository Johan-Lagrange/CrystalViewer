extends Button

const SAVED_LAYOUT_PATH := "user://layout.tres"

@export var _container: DockableContainer

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


func _on_load_pressed() -> void:
	var res = load(SAVED_LAYOUT_PATH)
	if res:
		_container.set_layout(res.clone())
	else:
		print("Error")
