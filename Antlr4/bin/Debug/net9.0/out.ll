@.fmt_i = private unnamed_addr constant [4 x i8] c"%d\0A\00"
@.fmt_f = private unnamed_addr constant [4 x i8] c"%f\0A\00"
@.fmt_i_in = private unnamed_addr constant [3 x i8] c"%d\00"
@.fmt_f_in = private unnamed_addr constant [3 x i8] c"%f\00"

declare i32 @printf(i8*, ...)
declare i32 @scanf(i8*, ...)
declare i32 @fflush(i8*)

define i32 @main() {
entry:
  %t0 = alloca i32
  store i32 0, i32* %t0
  %t1 = getelementptr [3 x i8], [3 x i8]* @.fmt_i_in, i32 0, i32 0
  call i32 (i8*, ...) @scanf(i8* %t1, i32* %t0)
  %t2 = load i32, i32* %t0
  %t3 = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0
  call i32 (i8*, ...) @printf(i8* %t3, i32 %t2)
  ret i32 0
}
