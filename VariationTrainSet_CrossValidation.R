library(neuralnet)
library(caTools)
library(plyr)
library(boot)
library(matrixStats)
library(rstudioapi)

#-------------------------Read csv file------------------------------------------------------
current_path <- getActiveDocumentContext()$path 
setwd(dirname(current_path ))
data <- read.delim("features2.csv",header=T, sep=";",dec=",") 
data <- within(data, rm("File_ID"))

#--------------Creation of the formula: grade = sum of other features----------------------
header <- names(data)
f <- paste(header[1:14], collapse = '+')
f <- paste(header[15], '~',f) # index 15 = grade (class)
f <- as.formula(f)

#--------------- Normalize values (max-min method)------------------------------------------
max <- apply(data,2,max)
min <- apply(data,2,min)
data <- as.data.frame(scale(data,center=min,scale=max-min))

#-------------------------------------Initializations------------------------------------------
set.seed(88)
error <- NULL
percentError <- NULL
k <- 50
ListMSE = list()
ListAccuracy = list()
pbar <- create_progress_bar('text') # Check progress of the whole process
pbar$init(k)

#--------------Establish the loop for splitting data randomly (vary train set from 30% to 80%)---------
# -------------------and train/test the net + calculate errors (50 folds--> k)------------------
for (j in 30:80){ # Dataset has 98 rows, so n ~ % 
  
  for(i in 1:k) {
    
    index <- sample(1:nrow(data), j)
    train <- data[index,]
    test <- data[-index,]
  
    nn <- neuralnet(f, data = train, hidden = c(2, 1), linear.output = F) # Train
    predicted.nn.values <- compute(nn, test[1:14]) # Test
    
  #--------------De-normalize values (from test set and predicted)-----------------
    predicted.values <- predicted.nn.values$net.result * (max[15] - min[15]) + min[15]   
    actual.values <- test[15] * (max[15] - min[15]) + min[15]   
    
    error[i] <- sum((actual.values - predicted.values) ^ 2) / nrow(test) # Calculate Error(RMSE)
    actual.values <- actual.values$Grade
    percentError[i] <- mean((actual.values - predicted.values) / actual.values * 100)
  }
  ListMSE[[j]] = error
  ListAccuracy[[j]] = 100 - abs(percentError)
  pbar$step()
}

#--------------------------NEURAL NETWORK QUALITY EVALUATION------------------------------------

#garson(nn)
Matrix.MSE = do.call(cbind, ListMSE)
Matrix.Accuracy = do.call(cbind, ListAccuracy)

#-------------Get average MSE and use BoxPlot to visualize it--------------------------

boxplot(Matrix.MSE[,31], xlab = "MSE", col = 'green', border = 'red',
        main = "MSE BoxPlot (traning set = 60%)", horizontal=TRUE)
boxplot(Matrix.MSE[,41], xlab = "MSE", col = 'yellow', border = 'orange',
        main = "MSE BoxPlot (traning set = 70%)", horizontal=TRUE)
boxplot(Matrix.MSE[,51], xlab = "MSE", col = 'pink', border = 'red',
        main = "MSE BoxPlot (traning set = 80%)", horizontal=TRUE)

#-------------Get average Accuracy and use BoxPlot to visualize it--------------------------

boxplot(Matrix.Accuracy[,31], xlab = "Accuracy (%)", col = 'green', border = 'red',
        main = "Accuracy BoxPlot (traning set = 60%)", horizontal=TRUE)
boxplot(Matrix.Accuracy[,41], xlab = "Accuracy (%)", col = 'yellow', border = 'orange',
        main = "Accuracy BoxPlot (traning set = 70%)", horizontal=TRUE)
boxplot(Matrix.Accuracy[,51], xlab = "Accuracy (%)", col = 'pink', border = 'red',
        main = "Accuracy BoxPlot (traning set = 80%)", horizontal=TRUE)

#---------------------Graphic of MSE (median) according to train set size------------------------

med = colMedians(Matrix.MSE)
X = seq(30,80)
plot (med~X, type = "l", xlab = "Size training set (%)", ylab = "Median MSE", 
      main = "Variation of MSE with training set size")

#--------------------Graphic of Accuracy (median) according to train set size--------------------

med2 = colMedians(Matrix.Accuracy)
X = seq(30,80)
plot (med2~X, type = "l", xlab = "Size training set (%)", ylab = "Median Accuracy", 
      main = "Variation of Accuracy with training set size")
