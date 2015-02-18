Fusion Boy
==========

This a Game Boy emulator written in C# and WPF that is a work in progress. You can resize the window to whatever size you'd like (fullscreen support not implemented yet), which is nice.

Things to still do:
* Input. You know, so you can actually play the games.
* Additional cartridge types. Currently, only MBC0 (Rom only) and MBC1 are supported.
* Fix graphics bugs.
* Sound.
* Fullscreen support.
* Save States.
* A Braid-style rewind system.


It currently passes the following Blarrg test roms.

![Alt text](/screenshots/fusionboy-cpu-instrs.png "CPU Instructions")

![Alt text](/screenshots/fusionboy-cpu-timing.png "CPU Instruction Timing")

![Alt text](/screenshots/fusionboy-mem-timing.png "Memory Timing")


Some random games.

![Alt text](/screenshots/fusionboy-battletoads.png "Battletoads")

![Alt text](/screenshots/fusionboy-kirby.png "Kirby's Dream Land")

![Alt text](/screenshots/fusionboy-tetris.png "Tetris")

Resources I'm using to develop this:
* [Pan Docs](http://problemkaputt.de/pandocs.htm)
* [Game Boy CPU Manual](http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf)
* [Game Boy Instruction Set](http://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html)
* [Blargg's Test Roms](http://gbdev.gg8.se/wiki/articles/Test_ROMs)
* [Higan](http://byuu.org/emulation/higan/)