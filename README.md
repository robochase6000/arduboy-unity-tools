# arduboy-unity-tools


this is a unity project that has some tools to ease arduboy development

Workflow for converting pngs to arduboy sprite data
1. drop png files into Assets/ExportedAssets folder (or any folder in the Assets folder really, but this one is set up to .gitignore the image files already)
2. open the unity project
3. select the SpriteProcessor scriptable object in unity (Assets/Data/SpriteProcessor.asset)
4. in the Inspector GUI for the Sprite Processor:
5. drag in the "Textures To Generate"
6. either set specific dimensions for each sprite in your sprite animation via the "Export Sprite Width/Height" fields, or check "Use Image Size For Sprite Dimensions" if you're exporting an unusually sized non-animating sprite.
7. click Generate Sprite Datas.  this spits out a file here Assets/ExportedAssets/spriteData.txt which has the sprite data in arduboy's expected format

the  SpriteProcessor can also generate placeholder game Characters for you
1. select the SpriteProcessor. in the Inspector GUI:
2. set the CharacterNames you want to use
3. set the CharacterStates you want to use
4. click "Create Characters"
5. this generates a bunch of images in Assets/ExportedAssets.  just garbage images, but they're all named correctly.  from there you can feedd them into the "Textures to Generate" list to generate arduboy sprite data for them.

