# XLORify-Sharp
A tool for creating XLOR files from NIntendo's binary font formats

#### Features
* __Converts BRFNT\BCFNT\BFFNT Character Code Map to XLOR file for use with FontCvtr from the official SDK__

#### Usage
__xlorify.exe infile outfile xlorname__

#### Why is it so Sharp?
I called it XLORify-Sharp because I had coded a similar program in Python from pieces of code of BRFNTify-Next by RoadrunnerWMC. It didn't work that well and because I was working on another program for font editing at the time, written in C#, I decided to use some of it's codebase to create a smaller tool.

#### HOW TO ACTUALLY USE IT TO RIP\EDIT FONTS:
##### REQUIREMENTS:
* __-Font files(brfnt\bcfnt\bffnt) (obviously!)__
* __-XLORify-Sharp (again, obviously!)__
* __-Font Converter(will be refered to as FontCvtr from now on)(from NintendoWare of specific platform you want to edit a font for), RVL and early version CTR can be used with GUI, while any newer versions will have to be operated though CLI__
* __-Image editor(I use Photoshop)__
* __-Patience__
##### INSTRUCTIONS:
* __1) Create XLOR from your font file with XLORify by typing "xlorify.exe {path to BxFNT} {where to save XLOR, you should pick "xlor" subfolder of your FontCvtr directory} {optional: XLOR internal name, pick something that you can recognise}"__
* __2) Edit produced xlor file with a text editor to add any charcters that you want(it's all in UTF-16BE, you can look up unicode character codes at unicode-table.com)(also remember in which order and where you put them, as these characters will show up as black spaces in the spritesheet after ripping)(make all necessary changes to character map NOW, or you'll have to redo a lot of stuff later)__
* __3) Convert your BxFNT to a BMP image using FontCvtr using your xlor as character ordering file. You can do this in GUI if it allows binary input, otherwise use CLI with this command "fontcvtr_console.exe -i bxfnt -if {path to your BxFNT} -o image -of {where to save spritesheet, extension should be bmp} -oo {path to xlor you generated with xlorify} -oi bmp"(note: in "bxfnt" replace "x" with appropriate platform letter)__
* __4) Edit font graphics however you want with your favourite image editor(note that some files will have a color palette, so you should use MSPaint)__
* __5) Use the GUI version of FontCvtr to convert edited image back to a binary font(don't forget to use the apropriate xlor!!!)__
* __6) Profit!!!__

Footnote on the version of FontCvtr you're supposed to use:
This applies only to CTR, but if your font is bffnt you should use the newer version, if it's the older bcfnt then you should utilize ver1.2 of FontCvtr(source of which you can find on GitHub(the gui won't compile, only the dll and cli, but that should be enough to do the thing anyway)).
P.S. You have to use the same version of FontCvtr for BOTH encoding AND decoding binary fonts
