@.fmt_i = private unnamed_addr constant [4 x i8] c"%d\0A\00"
@.fmt_f = private unnamed_addr constant [4 x i8] c"%f\0A\00"
declare i32 @printf(i8*, ...)

define i32 @main() {
entry:
  %t0 = alloca i32
  store i32 5, i32* %t0
  %t1 = alloca double
  store double 0.000000e+00, double* %t1
  %t2 = load i32, i32* %t0
  %t3 = add i32 %t2, 1
  store i32 %t3, i32* %t0
  %t4 = load i32, i32* %t0
  %t5 = icmp sgt i32 %t4, 5
  br i1 %t5, label %then0, label %else2
then0:
  %t6 = load i32, i32* %t0
  %t7 = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t7, i32 %t6)
  br label %endif1
else2:
  %t8 = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t8, i32 0)
  br label %endif1
endif1:
  br label %while.cond3
while.cond3:
  %t9 = load i32, i32* %t0
  %t10 = icmp slt i32 %t9, 10
  br i1 %t10, label %while.body4, label %while.end5
while.body4:
  %t11 = load i32, i32* %t0
  %t12 = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t12, i32 %t11)
  %t13 = load i32, i32* %t0
  %t14 = add i32 %t13, 1
  store i32 %t14, i32* %t0
  br label %while.cond3
while.end5:
  %t15 = alloca i32
  store i32 0, i32* %t15
  br label %for.cond6
for.cond6:
  %t16 = load i32, i32* %t15
  %t17 = icmp slt i32 %t16, 2
  br i1 %t17, label %for.body7, label %for.end8
for.body7:
  %t18 = load i32, i32* %t15
  %t19 = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t19, i32 %t18)
  %t20 = load i32, i32* %t15
  %t21 = add i32 %t20, 1
  store i32 %t21, i32* %t15
  br label %for.cond6
for.end8:
  ret i32 0
}
