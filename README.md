![divider](https://github.com/VIZ-Interactive/Depiction-Engine-Unity/assets/1084857/79a06c86-1a7f-4e5c-936f-b1bb833af3c1)


# Depiction Engine For Unity

![Presentation](https://github.com/VIZ-Interactive/Depiction-Engine-Unity/assets/1084857/ac3434d3-24ca-4db4-a49d-6cbeee49d3cb)


[Forum](https://vizinteractive.io/forum/depiction-engine/)

[Documentation](https://vizinteractive.io/docs/2023.0/depiction-engine-unity)

## Unity Specific Features
- Transform
	- Position objects at far greater distance from the world's origin with double (64bit) precision transforms and origin shifting, allowing you to create much larger projects.
- Shader
	- Shader Graph integration.
- Editor integration
    - Scene camera navigation around spherical planet.
    - Move to View / Align with View / Align View to Selected, all supported by double precision transforms.
    - Double click navigation in the hierarchy works even for double precision transforms.
    - Origin shifting supports multi camera layout (Although at significant performance cost).
	- Undo and Redo are automatically managed for things like object Creation / Destroy / Duplicate.
	- Initialization and Dipose states are provided to help identify the context under which the operation was requested. Search for InitializationContext and DisposeContext in the Documentation for more information.
