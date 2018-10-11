# Octopode - a Kraken Control Utility

![][1]

This software aims to replace the supplied software stack for Kraken X water
cooling solutions by NZXT. To save effort while maintaining a small memory
profile this software is written exclusively in C#.

Some of the reasons are:
- high software overhead for simple hardware solutions
- open source documentation of protocols and communication system ([see][2])
- alternative lightning modes
- removing the necessity for another electron app
- single bundled binary
- network requirements on CAM, with and without logins
- transparency in data collection

## What works already

- general monitoring functionality with permanent storage of performance metrics
- setting lightning modes for Rim and Logo lights (with white as fixed color)
- setting default performance curves for fans and pump individually
- benchmarking fans / pump speed. 

## What should work soon

- custom curves being saved permanently with up to 64 points
- lightning programming for colors, directions and other options enabled by CAM
- custom lightning programming for up to 70fps reactive modes
- benchmarking the micro controller

## What possible by the Kraken X micro controller

The Kraken takes about 72 commands per second for LEDs, this gives a nice and
smooth visual. Additionally the default CAM software only allows for 8
temperature settings while submitting 21 values. With the ability to store up
to 64 temperature points per module (fan and pump), you could add finer tuned
temperature control on the pump.

By the simplicity of the patching process and due to the fact that the micro
controller is known, it could be possible writing custom firmware for the Kraken
and similar NZXT setups. This would additionally allow to remove some flaws of
the controller and writing custom lightning modes directly onto the chip, rather
than issueing 70+ commands towards the pump. This could also free up the USB
header.

## What CAM can and this cannot (for now)

- monitoring your CPU, GPU temperature and set the RPM speed based on that
- enabling the LEDs to react to music and games
- enabling GPU overclocking (for some reason)
- writing temperature histories on CPU and GPU, both of which are only
  searchable with a NZXT account

# Contribution

This project is free to contribute. There is no contribution guide.

# License

This is MIT licensed. For license details, [see the license][3].

[1]: docs/octopode.png
[2]: PROTOCOL.md
[3]: LICENSE