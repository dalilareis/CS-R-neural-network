library(neuralnet)
library(caTools)
library(caret)
library(NeuralNetTools)
library(rstudioapi)

#-------------------------Read csv file------------------------------------------------------
current_path <- getActiveDocumentContext()$path 
setwd(dirname(current_path ))
data <- read.delim("features2.csv",header=T, sep=";",dec=",") 
apply(data,2,function(x) sum(is.na(x))) # Check for missing values
data <- within(data, rm("File_ID")) # Remove 1st column (string) before normalizing
summary(data$Grade) # Check data distribution
sum(data$Grade < 0.5)/nrow(data)*100 # 6%

#--------------- Normalize values (max-min method)------------------------------------------
max <- apply(data,2,max)
min <- apply(data,2,min)
data <- as.data.frame(scale(data,center=min,scale=max-min))

#----------------Split data into train & test sets-----------------------------------------
set.seed(101) # Ensure data is reproducible (same seed)
split <- sample.split(data$Num_Clicks, SplitRatio = 0.75)
train = subset(data, split == T)
test = subset(data, split == F)

#--------------Creation of the formula: grade = sum of other features----------------------
header <- names(data)
f <- paste(header[1:14], collapse = '+')
f <- paste(header[15], '~',f) # index 15 = grade (class)
f <- as.formula(f)

#-----------------------Train the Neural Network (using train set)-----------------------------
nn <- neuralnet(f,train,hidden = c(2,1), linear.output = F) 
plot(nn)
nn$result.matrix

#------------------------Variables -> generalized weights plots--------------------------------
garson(nn) # importance of variables (only for 1 hidden layer)
par(mfrow = c(2, 3))
gwplot(nn,selected.covariate="Num_Clicks") 
gwplot(nn,selected.covariate="Num_Double_Clicks") 
gwplot(nn,selected.covariate="AED") 
gwplot(nn,selected.covariate="ED")
gwplot(nn,selected.covariate="MA") 
gwplot(nn,selected.covariate="MV") 
gwplot(nn,selected.covariate="DMSL")
gwplot(nn,selected.covariate="ASA")
gwplot(nn,selected.covariate="SSA") 
gwplot(nn,selected.covariate="TBC") 
gwplot(nn,selected.covariate="DBC") 
gwplot(nn,selected.covariate="CD") 

#--------------------------NEURAL NETWORK QUALITY EVALUATION------------------------------------

#-------------------------Test NN (using test set)---------------------------------------------
predicted.nn.values <- compute(nn, test[1:14])

#-----------------Descaling for comparison (de-normalize all values)-------------------------
predicted.values <- (predicted.nn.values$net.result * (max[15] - min[15]) + min[15])
actual.values <- (test[15] * (max[15] - min[15]) + min[15])

#-----------------------------Calculate errors----------------------------------------------------
MSE <- sum((actual.values - predicted.values)^2) / nrow(test) #Scale-dependent
postResample(predicted.values, actual.values) # R^2, RMSE & MAE
#RMSE <- MSE ^ 0.5

actual.values <- actual.values$Grade
percentErrors = (actual.values - predicted.values)/actual.values * 100 #% erro relativo
MAPE <- mean(abs(percentErrors)) #Mean Absolute Percentage Error (scale-independent)
comparison <- data.frame(predicted.values, actual.values, percentErrors, accuracy = 100 - abs(percentErrors))
accuracy <- 100 - abs(mean(percentErrors))

#----------------------Discretization of Class --> Confusion Matrix----------------------------
tagActual <- as.numeric(actual.values >= 0.5)
tagPredicted <- as.numeric(predicted.values >= 0.5)
table(tagActual, tagPredicted)

