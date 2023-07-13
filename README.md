![divider](https://github.com/VIZ-Interactive/Depiction-Engine-Unity/assets/1084857/0c45072b-4a37-4b99-9f8c-df9847e0dbd5)


# Depiction Engine For Unity

![Presentation](https://vizinteractive.io/depiction-engine)

[Project page](https://vizinteractive.io/depiction-engine)

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

## Latest Release

[UnityPackage](https://github.com/VIZ-Interactive/Depiction-Engine-Unity/releases/download/2022.0a/DepictionEngine.unitypackage)

## Documentation

[Documentation](https://vizinteractive.io/docs/2022.0/depiction-engine-unity)

## Roadmap

## License

[MIT](https://en.wikipedia.org/wiki/MIT_License)
