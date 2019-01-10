library(neuralnet)
library(caTools)
library(plyr)
library(boot)
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
set.seed(400)
error <- NULL
percentError <- NULL
k <- 10
pbar <- create_progress_bar('text') # Check progress of the whole process
pbar$init(k)

#--------------Establish the loop for splitting data randomly (90% train set, 10% test)---------
# -------------------and train/test the net + calculate errors (10 times--> k)------------------
for(i in 1:k) {
  
  index <- sample(1:nrow(data),round(0.9*nrow(data)))
  train <- data[index,]
  test <- data[-index,]
  
  nn <- neuralnet(f, data = train, hidden = 3, algorithm = "rprop-", linear.output = F) # Train
  predicted.nn.values <- compute(nn, test[1:14]) # Test
  
#--------------De-normalize values (from test set and predicted)-----------------
  predicted.values <- predicted.nn.values$net.result * (max[15] - min[15]) + min[15]   
  actual.values <- test[15] * (max[15] - min[15]) + min[15]   
  
  error[i] <- sum((actual.values - predicted.values) ^ 2) / nrow(test) # Calculate Error (MSE)
  actual.values <- actual.values$Grade
  percentError[i] <- mean(((actual.values - predicted.values) / actual.values * 100))
  pbar$step()
}

garson(nn)

#--------------------------NEURAL NETWORK QUALITY EVALUATION------------------------------------

#-------------Get average MSE and use BoxPlot to visualize it--------------------------
meanPercentError <- mean(percentError)
meanAccuracy <- 100 - abs(mean(percentError))
boxplot(percentError,xlab='Relative Errors (%)',col='red', border='blue', names='%Error',
        main='Relative error for NN',horizontal=TRUE)
boxplot(100 - abs(percentError),xlab='Accuracy (%)',col='blue', border='red', names='Accuracy',
        main='Accuracy for NN',horizontal=TRUE)
meanMSE <- mean(error)
boxplot(error,xlab='Mean Square Error (MSE)',col='cyan', border='blue', names='Error (MSE)',
        main='Mean Square Error for NN',horizontal=TRUE)



