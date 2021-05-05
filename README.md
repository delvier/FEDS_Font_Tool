# FEDS_Font_Tool

The yet-to-be-implemented Fire Emblem DS Font Tool.

## File Specs

The Fire Emblem DS font files, especially ``fonts/alpha`` and ``fonts/talk`` are weird.

### Alpha and Talk

#### Header

#### Glyph
The binary data for a glyph is separated into blocks of five bytes.

The first byte indicates if the pixel to be written is transparent or not. A bit ``1`` is for transparent, and ``0`` for coloured.

The following four bytes indicate the amount of transparent pixels or the colour of a pixel. For example, a half-byte ``0xF`` means sixteen (i.e. 0xF + 1) transparent pixels or a pixel coloured F (white), depending on a particular bit of the first byte.

Note that the bits from the first byte should be read from the least-significant bit(bit 0), while the half-bytes with human-reading order: Let's see the next example.

* Example Raw data: ``55 0C 5D AE FF``
* The first byte in binary is ``01010101``.
0. Bit 0 is 1, and half-byte 0 is 0. Thus, the first one pixel is transparent.
1. Bit 1 is 0, and half-byte 1 is C. Thus, the following is a pixel with a colour C.
2. Bit 2 is 1, and half-byte 2 is 5. Thus, the following are transparent, and the number of them is 6 ( = 0x5 + 1).
3. Bit 3 is 0, and half-byte 3 is D. Thus, the following is a pixel with a colour D.
4. Bit 4 is 1, and half-byte 4 is A. Thus, the following are transparent, and the number of them is 11 ( = 0xA + 1).
5. Bit 5 is 0, and half-byte 5 is E. Thus, the following is a pixel with a colour E.
6. Bit 6 is 1, and half-byte 6 is F. Thus, the following are transparent, and the number of them is 16 ( = 0xF + 1).
7. Bit 7 is 0, and half-byte 7 is F. Thus, the following is a pixel with a colour F.

When the binary is deciphered, the pixel is drawn by following order on 16-by-16 cells:
* Pixels are drawn from the top row.
* For each row, they are drawn in order of: ``7 6 5 4 3 2 1 0 F E D C B A 9 8``. In other words, split by half, left first; for each part, right first.

##### Colour
The colours are tones of reds. The below are approximations, except for zero.
Half-byte | Colour
----------|-------
0x0 | (Transparent)
0x1 | #480808
0x2 | #501818
0x3 | #602828
0x4 | #683838
0x5 | #784848
0x6 | #805858
0x7 | #906868
0x8 | #987878
0x9 | #A08888
0xA | #B09898
0xB | #B8A8A8
0xC | #C8B8B8
0xD | #D0C8C8
0xE | #E0D8D8
0xF | #E8E8E8
