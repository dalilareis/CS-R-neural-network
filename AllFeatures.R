library(neuralnet)
library(caTools)
library(caret)
library(NeuralNetTools)
library(rstudioapi)

#-------------------------Read csv file------------------------------------------------------
current_path <- getActiveDocumentContext()$path 
setwd(dirname(current_path ))
data <- read.delim("features.csv",header=T, sep=";",dec=",") 
apply(data,2,function(x) sum(is.na(x))) # Check for missing values
data <- within(data, rm("File_ID")) # Remove 1st column (string) before normalizing

#--------------- Normalize values (max-min method)------------------------------------------
max <- apply(data,2,max)
min <- apply(data,2,min)
data <- as.data.frame(scale(data,center=min,scale=max-min))

#----------------Split data into train & test sets-----------------------------------------
set.seed(101)
split <- sample.split(data$Num_Clicks, SplitRatio = 0.75)
train = subset(data, split == T)
test = subset(data, split == F)

#--------------Creation of the formula: grade = sum of other features----------------------
header <- names(data)
f <- paste(header[1:26], collapse = '+')
f <- paste(header[27], '~',f) # index 27 = grade (class)
f <- as.formula(f)

#-----------------------Train the Neural Network (using train set)-----------------------------
nn <- neuralnet(f,train,hidden = c(2, 1), linear.output = F) 
plot(nn)
nn$result.matrix
garson(nn) # importance of variables (hidden layers cannot be a vector!)

#------------------------Variables -> generalized weights plots--------------------------------
par(mfrow = c(2, 3))
gwplot(nn,selected.covariate="Num_Clicks") 
gwplot(nn,selected.covariate="Num_Double_Clicks") 
gwplot(nn,selected.covariate="AED") 
gwplot(nn,selected.covariate="StD_AED") 
gwplot(nn,selected.covariate="ED")
gwplot(nn,selected.covariate="StD_ED")
gwplot(nn,selected.covariate="MA") 
gwplot(nn,selected.covariate="StD_MA") 
gwplot(nn,selected.covariate="MV") 
gwplot(nn,selected.covariate="StD_MV") 
gwplot(nn,selected.covariate="DMSL")
gwplot(nn,selected.covariate="StD_DMSL") 
gwplot(nn,selected.covariate="ASA")
gwplot(nn,selected.covariate="StD_ASA")
gwplot(nn,selected.covariate="SSA") 
gwplot(nn,selected.covariate="TBC") 
gwplot(nn,selected.covariate="StD_TBC") 
gwplot(nn,selected.covariate="DBC") 

#--------------------------NEURAL NETWORK QUALITY EVALUATION------------------------------------

#-------------------------Test NN (using test set)---------------------------------------------
predicted.nn.values <- compute(nn, test[1:26])

#-----------------Descaling for comparison (de-normalize all values)-------------------------
predicted.values <- (predicted.nn.values$net.result * (max[27] - min[27]) + min[27])
actual.values <- (test[27] * (max[27] - min[27]) + min[27])

#-----------------------------Calculate errors--------------------------------------------------
MSE <- sum((actual.values - predicted.values)^2) / nrow(test) 
postResample(predicted.values, actual.values) # R^2, RMSE & MAE
#RMSE <- MSE ^ 0.5

actual.values <- actual.values$Grade
percentErrors = (actual.values - predicted.values)/actual.values * 100
MAPE <- mean(abs(percentErrors)) #Mean Absolute Percentage Error (scale-independent)
comparison = data.frame(predicted.values, actual.values, percentErrors)
accuracy = 100 - abs(mean(percentErrors))

#----------------------Discretization of Class --> Confusion Matrix---------------------------
tagActual <- as.numeric(actual.values >= 0.5)
tagPredicted <- as.numeric(predicted.values >= 0.5)
table(tagActual, tagPredicted)

