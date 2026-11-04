# Image Classifier: Backpropagation MLP in C#

Multi-class image classification (forest, glacier, sea, street) using a custom
3-layer neural network (INeuron, HNeuron, ONeuron, NeuralNet) trained with backpropagation.

**Group:** Baniasia, Alexa Christel; Sario, Sarah Mae
**Course/Section:** CS345 F2 (Intelligent Systems 1)

## Data
Intel Image Classification (Kaggle):
https://www.kaggle.com/datasets/puneet6060/intel-image-classification

- 4 classes: forest, glacier, sea, street
- 60 or 100 training images per class (from seg_train)
- 40 unseen test images (10 per class, from seg_test)
- Images resized to 16x16 or 32x32, RGB values scaled to 0..1
- Random 80/20 train/test split on every run

The dataset is not included in this repository. Download it and arrange the
training images like this (class folders must contain only image files):

```
MyDataset\forest
MyDataset\glacier
MyDataset\sea
MyDataset\street
```

## Requirements
Windows, Visual Studio, .NET Framework 3.5 (Windows Forms).

## How to run
1. Open the .sln in Visual Studio and build (Ctrl+Shift+B).
2. In Form1.cs, set `int size = 16;` (or 32).
3. Press F5. Click "Create NN for Images" and select the MyDataset folder.
4. Click "train NN" and wait for the results message.
5. Click "Load Image", pick an image, then click "Classify".
6. Output files appear in bin\Debug:
   - `log_<size>_<count>img.csv`: training MSE every 10 epochs
   - `tuning_<size>_<count>img.csv`: test accuracy for each configuration
   - `results_<size>_<count>img.txt`: accuracy, precision and recall

   `<count>` is the total number of images (240 = 60 per class, 400 = 100 per class).

## Results
| Run | Images/class | Size | Best config (hidden/lr/epochs) | Test accuracy | Manual test (40) |
|---|---|---|---|---|---|
| 1 | 60 | 16x16 | 64 / 0.2 / 200 | 68.8% | 21/40 |
| 2 | 100 | 16x16 | 32 / 0.1 / 100 | 53.8% | 20/40 |
| 3 | 100 | 32x32 | 16 / 0.1 / 100 | 52.5% | 24/40 |

Each setup was run once. See the full report in `Report.pdf`.

## Files
- `NeuralNet.cs`, `INeuron.cs`, `HNeuron.cs`, `ONeuron.cs`: the provided network classes
- `Form1.cs`: data loading, 80/20 split, hyperparameter tuning, evaluation
- `results/`: log, tuning and results files from the three runs
- `Report.pdf`: full report with analysis
- `loss_chart.png`: training loss chart

## Repository
https://github.com/alexabaniasia/Real-World-Prediction-with-a-Custom-Multi-Layer-Perceptron-in-C-
