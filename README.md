# Speaker Identification System

A team project implementing a **Speaker Identification System** in C#.  
The system identifies speakers by comparing their voiceprints using **Dynamic Time Warping (DTW)** and an optimized pruning technique.

## Features
- **Voice Enrollment**: Register users by recording and storing their voice templates.
- **Speaker Identification**: Match unknown input against stored templates using DTW.
- **DTW Implementation**: Built from scratch for sequence alignment.
- **Optimized DTW**: Added pruning to reduce complexity from O(NM) to O(NW).

## Technologies
- C#
- .NET
- Dynamic Time Warping (DTW) Algorithm

## How it works
1. Users enroll by recording their voice (voiceprint stored as MFCC features).
2. For identification, the system compares the input voiceprint with stored templates.
3. DTW aligns sequences and calculates similarity.
4. Optimized DTW with pruning reduces search paths and speeds up matching.

## Outcomes
- Reduced computational complexity significantly using pruning.
- Gained experience in implementing and optimizing pattern-matching algorithms.
- Demonstrated application of algorithms in a **real-world biometric system**.

## Contributions
- Implemented core DTW algorithm in C#.
- Engineered pruning optimization.
- Built backend logic for enrollment and identification phases.
