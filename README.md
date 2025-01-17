Project from 2020. Reuploaded for backup.

PBR shading using T, TT, TRT components (Hair shading model)
Deep Opacity Maps for self shadowing. Casts and receives shadows with the environment.
Stochastic transparency in a single pass, instead of multipass transparency that was shown in amd papers.
This also allows for faster deferred shading, opacity maps and other passes to be combined for all hair instances on screen.
Basic Physics (Verlet/Langerian) using compute including self collision (3D volume occupancy).
Less than 1ms budget for thousands of strands with all the effects on.

video: https://www.youtube.com/watch?v=7NaJJ0MrkXg

![image](https://github.com/user-attachments/assets/360ffbda-f1fc-47d6-946a-d0ed4c391c2f)

