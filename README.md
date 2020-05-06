# jackcess-net

This is a port of version 1.2.5 of the Jackcess Java library, which is a pure Java library for reading from and writing to Microsoft Access databases (versions 2000-2007). It also has the capability to read Microsoft Money files in their native format.

This version allows the same access from .NET. It was inspired by the sunriise project: http://sunriise.sourceforge.net/

## Why not a more current version of Jackcess?

I was using sunriise to read my Microsoft Money database using Java, but I really wanted was to write a cross platform money management app (targetting Android and Windows UWP specifically), that would allow direct import from Microsoft Money. 
Whilst it may have been possible to incorporate the sunriise jar into an Android app, it wouldn't easily work under Windows, and there are no other tools for reading MS Money files in Windows.

Unfortunately, sunriise is quite old and uses that older version of Jackcess. Also, my requirements for the library were minimal (reading the .mny file once), so using a version that I knew met my needs seemed like the correct approach. 
It was also smaller and therefore required less porting effort, and the Sharpen Eclipse plugin was of a similar vintage and would be less likely to struggle with newer Java features in later Jackcess releases.

## Warning!!!

My use and testing of this library is limited to reading Microsoft Money files. 

I do NOT know if it works for any other purpose. 

USE AT YOUR OWN RISK!

## Thanks

This would not have been possible without the the following resources:

- Jackcess! https://jackcess.sourceforge.io/ (or maybe more specifically https://web.archive.org/web/20110831152637/http://jackcess.sourceforge.net/)
- Paul Du Bois and his blog pst https://pauldb-blog.tumblr.com/post/14916717048/a-guide-to-sharpen-a-great-tool-for-converting
- Eclipse and the Sharpen Eclipse plugin (alas, no longer available)
- The Sharpen C# utility classes to map Java core classes to C# core classes (plus the Sharpen plugin!) from ngit: https://github.com/mono/ngit
