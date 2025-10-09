@.fmt_i = private unnamed_addr constant [4 x i8] c"%d\0A\00"
@.fmt_f = private unnamed_addr constant [4 x i8] c"%f\0A\00"
declare i32 @printf(i8*, ...)

define i32 @main() {
entry:
  %t0 = alloca i32
  store i32 5, i32* %t0
  %t1 = alloca double
  store double 1.4, double* %t1
  %t2 = load i32, i32* %t0
  %t3 = add i32 %t2, 13
  store i32 %t3, i32* %t0
  %t4 = load i32, i32* %t0
  %t5 = icmp sgt i32 %t4, 5
  br i1 %t5, label %then0, label %else2
then0:
  %t6 = load double, double* %t1
  %t7 = getelementptr [4 x i8], [4 x i8]* @.fmt_f, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t7, double %t6)
  br label %endif1
else2:
  %t8 = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t8, i32 0)
  br label %endif1
endif1:
  ret i32 0
}
