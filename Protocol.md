# Command Patterns for Kraken

{{toc}}

## Message Types

By the looks of it, the Kraken X62 communicates by two command types:
- ``0x02`` Control Command
- ``0x04`` Status Command

There is a third message pattern, which has yet to be determined what it does.
To be frank, it is a fixed value, it makes numerically no sense to me right now.

# Control Commands

## Colors

Color Format is GRB:

```csharp
    color[1] = (byte) ((colorPattern[i, j/3] & 0xFF_00_00) >> 16);
    color[0] = (byte) ((colorPattern[i, j/3] & 0x00_FF_00) >> 8 );
    color[2] = (byte) ((colorPattern[i, j/3] & 0x00_00_FF));
```

## Pump Control & Fan Control

The message looks as following:

```
0x02  ; control command
0x4d  ; speed control
0x40  ; pump control, alternatively 0x00 for fan control
0x00  ; could be nothing, could be anything.
0x1e  ; decimal for 30, lowest setting, alternatively 0x64 for highest
```

The last byte determines the speed between 30% to 100%.

## Light Control

This is the complicated one, which takes quite a while to untangle - even with
disassembling NXZTs DLLs. The report size seems to be ``new byte[65]``

```
0x02  ; control command
0x4c  ; light control, which begs to wonder what 0x4a is
0x13  ; 0b0001_0000 is directional parameter
      ; 0b0000_1000 is a binary option solely for "alternating" light option
      ; 0b0000_0111 is 'iChannelMode' which is still a mystery
0x06  ; pulse mode, see light mode table
0x25  ; 0b111_00_000 is the light index number (for multiple colored light)
      ; 0b000_11_000 is the LED group size, which exists only for marquee width
      ; 0b000_00_111 is the speed of animation between 0-4
0xFF  ; Green Color for Text
0xFF  ; Red Color for Text
0xFF  ; Blue Color for Text
0xFF  ; Red Color for 1st (?) LED
0xFF  ; Green Color for 1st (?) LED
0xFF  ; Blue Color for 1st (?) LED
...
```

It seems to fit (more or less randomly) 20 LEDs for controlling (60 bytes for
coloring divided by 3), whereas probably only 9 (NZXT Logo + 8 Rim Lights) are
used. There seems to be a bug in which a Solid color per LED can be set and some
lightning modes seem to follow these without bothering - which (currently) can
be abused to creates effects that seemed impossible. Additionally this begs the
question if the firmware could handle more per LED effects.

### LED Modes

| Mode            | Byte     |
|-----------------|----------|
| Solid           | ``0x00`` |
| Fading          | ``0x01`` |
| SpectrumWave    | ``0x02`` |
| Marquee         | ``0x03`` |
| CoveringMarquee | ``0x04`` |
| Alternating     | ``0x05`` |
| Pulse           | ``0x06`` |
| Breathing       | ``0x07`` |
| Alert           | ``0x08`` |
| Candle          | ``0x09`` |
| Unknown         | ``0x0A`` |
| RPM             | ``0x0B`` |
| Wings           | ``0x0C`` |
| Wave            | ``0x0D`` |
| Audio           | ``0x0E`` |
| Halt            | ``0x0F`` |

Notice: Halt is no real mode, but sending an invalid coloring mode simply stops
the animation. Maybe this could be abused, but since you're able to set all LEDs
individually it seems to have little additional value.

## Turning LEDs off

```
0x02  ; control message
0x4c  ; light control
0x01  ; iChannelIndex - likely which LED should be turned off
```


# Status Messages

The Kraken x62 sends constantly status messages in a fixed format:

```
0x04  ; status message
0x20  ; liquid temperature in °C  (32°C)
0x08  ; liquid temperature decimal places in °C (.8°C)
0x01  ; most significant bit for fan speed (0x02 << 8)
0xFC  ; least significant bit for fan speed (0x02 << 8 | 0x08) -> 508 RPM
0x09  ; most significant bit for pump speed (0x09 << 8)
0x9A  ; least significant bit for pump speed (0x09 << 8 | 0x9A) -> 2458 RPM
0x00  ; checking flag, has to be 0
0x00  ; checking flag, has to be 0
0x00  ; checking flag, has to be 0
0x81  ; device number, seems to be random per device
0x02  ; firmware digit
0x00  ; firmware dot
0x01  ; firmware decimal place 1
0x08  ; firmware decimal place 2 -> 2.18
0x1E  ; checking byte (whatever it does)
0x00  ; empty for no other reason than being empty
```

CAMs signature check looks as following:

```csharp
(
     recievedData[7]  == (byte) 0 &&
     recievedData[8]  == (byte) 0 &&
     recievedData[9]  == (byte) 0 &&
     recievedData[16] == (byte) 0 &&
    (recievedData[15] == (byte) 30 || recievedData[11] == (byte) 2)
)
```

In short: Index 7, 8, 9, 16 have to be 0 and index 15 has to be either 30
or index 11 has to be 2 - probably an old version check.

## The Bug Message

Sometimes it spits an unknown message:

```
0x04  ; status message
0xD5  ; 
0xF6  ; 
0x9C  ; 
0xAB  ;
0x90  ;
0xDD  ;
0x64  ;
0x4C  ;
0x5F  ;
0x8F  ;
0xB8  ;
0x2B  ;
0xAF  ;
0x3D  ;
0xE1  ;
0x66  ;
0x00  ;
```