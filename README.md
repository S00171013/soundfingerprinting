# VGMSeeker
A simple "Shazam-like" song recognition WPF application coded in C#. This app serves as the implementation segment of my 4th year project on music recognition technology.
This repository has been forked from Sergiu Ciumac's [soundfingerprinting](https://github.com/AddictedCS/soundfingerprinting) repository. 

VGMSeeker's audio fingerprinting and storage capabilities were made possible 
by the [SoundFingerprinting](https://www.nuget.org/packages/SoundFingerprinting/), [_SoundFingerprinting.Emy_](https://www.nuget.org/packages/SoundFingerprinting.Emy/)
and [SoundFingerprinting.Audio.Naudio](https://www.nuget.org/packages/SoundFingerprinting.Audio.NAudio/) libraries authored by Sergiu Ciumac.

### Third Party Dependencies
Additional C# Libraries used by _VGMSeeker_.
* [NAudio](https://www.nuget.org/packages/NAudio) used to record microphone audio.
* [NAudio.Lame](https://www.nuget.org/packages/NAudio.Lame/) used to enable fingerprinting of MP3 files.
* [TagLib#](https://github.com/mono/taglib-sharp) used to extract metadata from MP3 files.
* [Microsoft.Asp.Net.WebApi.Client](https://dotnet.microsoft.com/apps/aspnet/apis) used to create a connection to the _vgmdb.info_ web API.
* [Newtonsoft.Json](https://www.newtonsoft.com/json) used to parse album image data from _vgmdb.info_ response data.
Additional services used by _VGMSeeker_.
* [VGMdb API](https://vgmdb.info/) used to find album art for a matched track from [VGMdb](https://vgmdb.net/).

Links to the third party libraries used by _soundfingerprinting_ project.
* [LomontFFT](http://www.lomont.org/Software/Misc/FFT/LomontFFT.html)
* [ProtobufNet](https://github.com/mgravell/protobuf-net)

### License
The framework is provided under [MIT](https://opensource.org/licenses/MIT) license agreement.

&copy; Soundfingerprinting, 2010-2019, sergiu@emysound.com
