
This project contains the code used for the _Sketch into Metaverse_ demo presented at AIUK2023. 

After running the demo on the host machine, the user puts on the headset and enters a virtual living room. In front of them, there is a cube labeled `Sketch Space`. 
1. __Sketch__: The user can use the right-hand controller to freely draw a chair sketch in the sketch space by pressing the `Sketch` trigger. During this process, users can use the `Grab` trigger at any time to rotate the entire sketch space, and use the `Undo` button on the left-hand controller to undo the last stroke. (Please refer to the operation guide below for the triggers and buttons)
2. __Search__: Once finished, the user can click the `Search` button on the TV using the `Click Button` on the right-hand controller to trigger a search. The top 1 search result will immediately appear in the `Sketch Space` cube. If the user want to see more results, press the `More Results` button on the left-hand controller. Pressing it again will hide the additional results. 
   1. The user can then select any model using the `Click Button`, and the chosen model will be displayed in the green area next to the table.
   2. If the search results are unsatisfactory, the user can continue drawing on the existing sketch and search again. Alternatively, they can click the `Clear` button on the TV to delete the current sketch and start over.
3. __End__: When finishing the game, click the `Exit` button on the door.

![game](game.gif)
Please refer to [the demo video](https://www.youtube.com/watch?v=bwabdXnS-Zo&t=73s&ab_channel=LingLuo) for the complete process after running the demo.

The controller operation guide is shown in the following figure and is also visible in the virtual room.

![controller operation](instructions.png)

Platform:

- Windows system: Unity + Visual Studio Code
- Oculus Rift: 1 headset + 2 hand controllers

The demo project consists of two parts: 
1. `retrieval_inference`: Backend inference code based on Python 
2. `Sketch_VR`: VR interface using Unity

To run the demo:
# Step 1: retrieval_inference

Open `retrieval_inference` in Visual Studio Code. 
Create your own conda environment, then install the necessary packages by running:
```shell
pip install -r requirements.txt
```
Run `main.py` from `retrieval_inference`.

# Step 2: Sketch_VR_demo

First, set up the Oculus environment and ensure it is functioning properly.

Second, download [the chair object files](https://pan.baidu.com/s/14Gh6p3Ix_Ylm70KZeBR9oQ?pwd=wjh9) with password `wjh9` and unzip the downloaded `ShapeNetCore.v2.zip` under the current `Sketch_VR_demo` directory.

If you want to run the demo directly, you can download [the executable file](https://drive.google.com/file/d/1eu6ajkRmwHDkoizROvMMcdXolq6iIkhc/view?usp=sharing) and extract the downloaded `game.zip` into the current `Sketch_VR_demo` directory. Then start the game by running `VR Sketch.exe`

The correct directory hierarchy structure is as follows:

```
- retrieval_inference
- Sketch_VR_demo
    - game
        - VR Sketch.exe: The executable file of this game.
        - ...
    - ShapeNetCore.v2
        - 03001627: chair category of ShapeNetCore.v2 dataset
    - demo_savedir: The location where the VR sketch is saved.
    - Sketch_VR: The code repository for Unity game development.
    - ...
```

If you want to continue editing this demo, please open the `Sketch_VR` __subdirectory__ in Unity. You can also download the original project from [Baidu Disk](https://pan.baidu.com/s/1mD2VXbqpny1WSTFmaaCwrw?pwd=b4qp) with password `b4qp`.






