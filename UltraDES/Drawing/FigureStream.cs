using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;

namespace UltraDES
{
    internal static class FigureStream
    {
        public static void WriteSVGHeader(StreamWriter file, double width, double heigth)
        {
            string widthString = Drawing.round(width, 0);
            string heigthString = Drawing.round(heigth, 0);

            file.WriteLine("<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"no\"?> ");
            file.WriteLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\" \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\"> ");
            file.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xml:space=\"preserve\"" +
                    " width=\"" + widthString + "px\" height=\" " + heigthString + "px\" viewBox=\"0 0 " + widthString + " " + heigthString + "\" >  ");
        }

        public static void WriteLatexHeader(StreamWriter file, double width, double heigth)
        {
            file.WriteLine("\\centering");
            file.WriteLine("\\begin{tikzpicture}[scale=0.3, x=0.1 cm, y=0.1 cm]");
            file.WriteLine("\\tikzstyle{every node}+=[inner sep=0pt]");
        }

        public static void WriteSVGEnd(StreamWriter file)
        {
            file.WriteLine("</svg>");
        }

        public static void WriteLatexEnd(StreamWriter file)
        {
            file.WriteLine("\\end{tikzpicture}");
        }

        public static void drawSVGState(StreamWriter file, DrawingState state, int radius)
        {
            string y = Drawing.round(state.position.Y);
            string x = Drawing.round(state.position.X);

            file.WriteLine("\t<circle cx=\"" + x + "\" cy=\"" + y + "\" r=\"" + radius + "\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" />");

            if (state.IsMarked)
            {
                file.WriteLine("\t<circle cx=\"" + x + "\" cy=\"" + y + "\" r=\"" + (radius - 4) + "\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" />");
            }

            int fontSize = 17;
            Vector gap = new Vector(0, fontSize / 3);
            writeTextSVG(file, state.position, state.Alias, 0, gap, "none", fontSize);
        }

        public static void drawLatexState(StreamWriter file, DrawingState state, int radius, string fontSize)
        {
            string y = Drawing.round(state.position.Y);
            string x = Drawing.round(state.position.X);

            file.WriteLine("\\draw [black, line width= 0.8pt] (" + x + "," + y + ") circle (" + radius + ");");

            if (state.IsMarked)
            {
                file.WriteLine("\\draw [black, line width= 0.8pt] (" + x + "," + y + ") circle (" + (radius - 4) + ");");
            }
            writeTextLatex(file, state.position, state.Alias, fontSize, 0);
        }

        public static void drawSVGArrow(StreamWriter file, Vector arrowLocation, double arrowInclination, Vector inclinationReference)
        {

            int arrowLength = 15;
            double arrowAngle = Math.PI / 12;                //angulo de abertura da seta 15 graus
            Vector firstPosition = new Vector();
            Vector secondPosition = new Vector();
            double arrowInclinationDegrees = -(arrowInclination * 180) / Math.PI;

            //calculo das cordenadas dos vertices da seta
            firstPosition.X = (arrowLocation.X) + arrowLength;
            firstPosition.Y = (arrowLocation.Y) + arrowLength * Math.Tan(arrowAngle);

            secondPosition.X = firstPosition.X;
            secondPosition.Y = arrowLocation.Y - arrowLength * Math.Tan(arrowAngle);

            string x1 = Drawing.round(arrowLocation.X);
            string y1 = Drawing.round(arrowLocation.Y);
            string x2 = Drawing.round(firstPosition.X);
            string y2 = Drawing.round(firstPosition.Y);
            string x3 = Drawing.round(secondPosition.X);
            string y3 = Drawing.round(secondPosition.Y);

            string xi = Drawing.round(inclinationReference.X);
            string yi = Drawing.round(inclinationReference.Y);
            string anglei = Drawing.round(arrowInclinationDegrees);

            //escreve em arquivo SVG
            file.WriteLine("\t<polygon points=\"" + x1 + "," + y1 + " " + x2 + "," + y2 + " " + x3 + "," + y3 +
                "\" style=\"fill:black;stroke:black;stroke-width:3\" transform =\"rotate(" + anglei + " " + 
                xi + "," + yi + ")\" />");

        }

        public static void drawLatexArrow(StreamWriter file, Vector arrowLocation, double arrowInclination, Vector inclinationReference)
        {
            int arrowLength = 15;
            double arrowAngle = Math.PI / 12;                //angulo de abertura da seta 15 graus
            Vector firstPosition = new Vector();
            Vector secondPosition = new Vector();
            double arrowInclinationDegrees = Math.Round((arrowInclination * 180) / Math.PI, Constants.NUMBER_OF_DIGITS_TO_ROUND);// (inclinacaoSeta * 180) / Math.PI;

            //calculo das cordenadas dos vertices da seta
            firstPosition.X = (arrowLocation.X) + arrowLength;
            firstPosition.Y = (arrowLocation.Y) + arrowLength * Math.Tan(arrowAngle);

            secondPosition.X = firstPosition.X;
            secondPosition.Y = arrowLocation.Y - arrowLength * Math.Tan(arrowAngle);

            string x1 = Drawing.round(arrowLocation.X);
            string y1 = Drawing.round(arrowLocation.Y);
            string x2 = Drawing.round(firstPosition.X);
            string y2 = Drawing.round(firstPosition.Y);
            string x3 = Drawing.round(secondPosition.X);
            string y3 = Drawing.round(secondPosition.Y);

            string xi = Drawing.round(inclinationReference.X);
            string yi = Drawing.round(inclinationReference.Y);
            string anglei = Drawing.round(arrowInclinationDegrees);

            //escreve no arquivo .txt
            file.WriteLine("\\fill [black, rotate around={" + anglei + ":(" + xi + "," + yi + ")} ] (" +
                x1+","+y1 + ") -- (" + x2+","+y2 + ") -- (" + x3+","+y3 + ");");
        }

        public static void drawSVGLine(StreamWriter file, Vector originPosition, Vector destinationPosition, double inclination, Vector inclinationReference)
        {
            string x1 = Drawing.round(originPosition.X);
            string y1 = Drawing.round(originPosition.Y);
            string x2 = Drawing.round(destinationPosition.X);
            string y2 = Drawing.round(destinationPosition.Y);

            string xi = Drawing.round(inclinationReference.X);
            string yi = Drawing.round(inclinationReference.Y);

            string inclinationDegrees = Drawing.round((inclination * 180) / Math.PI);

            file.WriteLine("\t<line x1=\"" + x1 + "\" y1=\"" + y1 + "\" x2=\"" + x2 + "\" y2=\"" + y2 + 
                "\" style=\"stroke:black;stroke-width:1\" transform =\"rotate(" + 
                inclinationDegrees + " " + (xi) + "," + yi + ")\" />");
        }

        public static void drawLatexLine(StreamWriter file, Vector originPosition, Vector destinationPosition, double inclination, Vector inclinationReference)
        {
            string x1 = Drawing.round(originPosition.X);
            string y1 = Drawing.round(originPosition.Y);
            string x2 = Drawing.round(destinationPosition.X);
            string y2 = Drawing.round(destinationPosition.Y);

            string xi = Drawing.round(inclinationReference.X);
            string yi = Drawing.round(inclinationReference.Y);

            string inclinationDegrees = Drawing.round(-(inclination * 180) / Math.PI);

            //escreve arquivo latex

            file.WriteLine("\\draw [black, line width= 0.8pt, rotate around={" + inclinationDegrees + ":(" + xi + "," + yi + 
                ")}] (" +  x1 + "," + y1 + ") -- (" + x2 + "," + y2 + ");");

        }

        public static void drawLineTransition(StreamWriter file, DrawingState origin, DrawingState destination, 
            string transitionName, string fontFill, int fontSize)
        {
            Vector length, originPoint, destinationPoint, arrowPoint;
            Vector textPoint = new Vector();
            Vector fixTextPosition = new Vector();
            double x, y, textOffset = 0, transitionGap = 0;
            double stateAngle = 0, arrowAngleOffset = 0, arrowInclination = 0;

            length = destination.position - origin.position;
            stateAngle = Math.Atan2(-length.Y, length.X);

            if (stateAngle < 0) stateAngle += 2 * Math.PI;

            if (origin.isAOrigin(destination))
            {
                transitionGap = Constants.TRANSITION_OFFSET;
                arrowAngleOffset = Math.Atan2(transitionGap, length.Length - 
                                                Constants.STATE_RADIUS - Constants.DISTANCE);

                if (arrowAngleOffset < 0) arrowAngleOffset += 2 * Math.PI;
            }

            x = origin.position.X + Constants.STATE_RADIUS + Constants.DISTANCE;
            y = origin.position.Y - transitionGap;
            originPoint = new Vector(x, y);

            x = origin.position.X + length.Length - Constants.STATE_RADIUS - Constants.DISTANCE;
            y = originPoint.Y;
            destinationPoint = new Vector(x, y);

            x = origin.position.X + Math.Cos(stateAngle + arrowAngleOffset) * (length.Length - Constants.STATE_RADIUS - Constants.DISTANCE);
            y = origin.position.Y - Math.Sin(stateAngle + arrowAngleOffset) * (length.Length - Constants.STATE_RADIUS - Constants.DISTANCE);
            arrowPoint = new Vector(x, y);

            arrowInclination = stateAngle + Math.PI;

            drawSVGLine(file, originPoint, destinationPoint, -stateAngle, origin.position);
            drawSVGArrow(file, arrowPoint, arrowInclination, arrowPoint);

            textOffset = Math.Atan2(Constants.TEXT_OFFSET + transitionGap, length.Length / 2);

            if (textOffset < 0)
            {
                textOffset += 2 * Math.PI;
            }

            //correcão do posicionamnto do texto quando angulo vara entre 90 e 270 graus
            if (stateAngle >= Math.PI / 2 && stateAngle <= 3 * Math.PI / 2)
            {
                fixTextPosition.X = -Math.Sin(stateAngle) * Constants.TEXT_OFFSET;
                fixTextPosition.Y = -Math.Cos(stateAngle) * Constants.TEXT_OFFSET;
            }

            x = origin.position.X + Math.Cos(textOffset + stateAngle) * (length.Length / 2) + fixTextPosition.X;
            y = origin.position.Y - Math.Sin(textOffset + stateAngle) * (length.Length / 2) + fixTextPosition.Y;

            Vector gap = new Vector();
            writeTextSVG(file, textPoint, transitionName, stateAngle, gap, fontFill, fontSize);
        }

        public static void drawLineTransitionLatex(StreamWriter file, DrawingState origin, DrawingState destination, string transitionName, string fontSize)
        {
            Vector length;
            Vector arrowOrigin = new Vector();
            Vector arrowDestination = new Vector();
            Vector arrowPoint = new Vector();
            Vector textPoint = new Vector();
            double textOffset = 0, transitionGap = 0, stateAngle = 0;
            double arrowAngleOffset = 0, arrowInclination = 0;

            length = destination.position - origin.position;
            stateAngle = Math.Atan2(-length.Y, length.X);

            if (stateAngle < 0) stateAngle += 2 * Math.PI;

            if (origin.isAOrigin(destination))
            {
                transitionGap = Constants.TRANSITION_OFFSET;
                arrowAngleOffset = Math.Atan2(transitionGap, length.Length - 
                                                Constants.STATE_RADIUS - Constants.DISTANCE);

                if (arrowAngleOffset < 0) arrowAngleOffset += 2 * Math.PI;
            }

            arrowOrigin.X = origin.position.X + Constants.STATE_RADIUS + Constants.DISTANCE;
            arrowOrigin.Y = origin.position.Y - transitionGap;

            arrowDestination.X = origin.position.X + length.Length - Constants.STATE_RADIUS - Constants.DISTANCE;
            arrowDestination.Y = arrowOrigin.Y;


            arrowPoint.X = origin.position.X + Math.Cos(stateAngle + arrowAngleOffset) * (length.Length - Constants.STATE_RADIUS - Constants.DISTANCE);
            arrowPoint.Y = origin.position.Y - Math.Sin(stateAngle + arrowAngleOffset) * (length.Length - Constants.STATE_RADIUS - Constants.DISTANCE);

            arrowInclination = stateAngle + Math.PI;

            drawLatexLine(file, arrowOrigin, arrowDestination, stateAngle, origin.position);
            drawLatexArrow(file, arrowPoint, -arrowInclination, arrowPoint);

            textOffset = Math.Atan2(Constants.TEXT_OFFSET + transitionGap, length.Length / 2);

            if (textOffset < 0) textOffset += 2 * Math.PI;

            textPoint.X = origin.position.X + Math.Cos(-textOffset + stateAngle) * (length.Length / 2);
            textPoint.Y = origin.position.Y - Math.Sin(-textOffset + stateAngle) * (length.Length / 2);

            writeTextLatex(file, textPoint, transitionName, fontSize, -stateAngle);
        }

        public static void drawCurveTransition2(StreamWriter file, DrawingState origin, DrawingState destination, string transitionName, string fontFill, int fontSize)
        {
            Vector stateDistance = destination.position - origin.position;
            double curveHeight;
            double transitionInclination = Math.Atan2(-stateDistance.Y, stateDistance.X);
            double startArrowAngle = 0;
            double endArrowAngle = 0;
            double offset = 0;
            double arrowAngleFix;
            double textAngleFix = 0;
            double textPointFix = 0;
            double arrowAngle;
            double ArcLength;
            Vector startArcPoint = new Vector();
            Vector destinationArcPoint = new Vector();
            string startArcBeginRef;

            if (transitionInclination < 0) transitionInclination += 2 * Math.PI;

            double transitionIncDegree = -Math.Round(180 * transitionInclination / Math.PI);

            startArcBeginRef = Drawing.round(origin.position.X + Constants.STATE_RADIUS + Constants.DISTANCE, 0);
            ArcLength = stateDistance.Length - 2 * (Constants.STATE_RADIUS + Constants.DISTANCE);
            curveHeight = -Math.Round(ArcLength / 3);

            offset = -Constants.TRANSITION_OFFSET;
            startArrowAngle = Math.Atan2(-offset, (Constants.STATE_RADIUS + Constants.DISTANCE));
            endArrowAngle = Math.Atan2(-offset, (Constants.STATE_RADIUS + Constants.DISTANCE + ArcLength));

            arrowAngleFix = Math.PI - Math.Atan2(-curveHeight, ArcLength / 2);
            arrowAngle = transitionInclination + arrowAngleFix;
            
            startArcPoint.X = origin.position.X + Math.Cos(transitionInclination + startArrowAngle) * (Constants.STATE_RADIUS + Constants.DISTANCE);
            startArcPoint.Y = origin.position.Y - Math.Sin(transitionInclination + startArrowAngle) * (Constants.STATE_RADIUS + Constants.DISTANCE);

            destinationArcPoint.X = origin.position.X + Math.Cos(transitionInclination + endArrowAngle) * (Constants.STATE_RADIUS + Constants.DISTANCE + ArcLength);
            destinationArcPoint.Y = origin.position.Y - Math.Sin(transitionInclination + endArrowAngle) * (Constants.STATE_RADIUS + Constants.DISTANCE + ArcLength);

            string p1 = Drawing.round(origin.position.Y + offset);
            string x1 = Drawing.round(origin.position.X);
            string y1 = Drawing.round(origin.position.Y);

            file.WriteLine("\t<path d=\"M " + startArcBeginRef + " " + p1 + " q " + Math.Round(ArcLength / 2)
                + " " +  curveHeight + " " + Math.Round(ArcLength) + " " + 0 + 
                "\" stroke=\"black\" stroke-width=\"1\" fill=\"none\"  transform =\"rotate(" + 
                transitionIncDegree + " " + x1 + "," + y1 + ")\" />");

            drawSVGArrow(file, destinationArcPoint, arrowAngle, destinationArcPoint);

            textPointFix = Constants.TEXT_OFFSET;
            if (transitionInclination > Math.PI / 2 && transitionInclination < 3 * Math.PI / 2)
            {
                textPointFix = Constants.TEXT_OFFSET + 6;
            }

            Vector textPoint = new Vector();
            double distanceArrowOriginText = Math.Sqrt((-curveHeight / 2 + textPointFix) * (-curveHeight / 2 + textPointFix) + (ArcLength / 2) * (ArcLength / 2));
            textAngleFix = Math.Atan2(-curveHeight / 2 + textPointFix, ArcLength / 2);

            textPoint.X = startArcPoint.X + Math.Cos(transitionInclination + textAngleFix) * distanceArrowOriginText;
            textPoint.Y = startArcPoint.Y - Math.Sin(transitionInclination + textAngleFix) * distanceArrowOriginText;
            textPoint = Drawing.RoundVector(textPoint);

            Vector gap = new Vector();
            writeTextSVG(file, textPoint, transitionName, transitionInclination, gap, fontFill, fontSize);
        }

        public static void drawCurveTransitionLatex(StreamWriter file, DrawingState origin, DrawingState destination, string transitionName, string fontSize)
        {
            Vector startArc = new Vector();
            Vector arcDestination = new Vector();
            Vector firstArcPoint = new Vector();
            Vector secondArcPoint = new Vector();
            Vector statesDistance = new Vector();
            Vector transitionNamePoint = new Vector();
            Vector distanceArrowDestination = new Vector();           //distancia entre estado origem e destino seta
            Vector arrowPoint = new Vector();
            double startArcAngle = Math.PI / 8;
            double stateAngle;
            double arrowAnglePointFix;
            double arrowInclination, arrowAngleFix;
            string stateAngleDegree;

            statesDistance = destination.position - origin.position;
            stateAngle = Math.Atan2(statesDistance.Y, statesDistance.X);

            if (stateAngle < 0) stateAngle += 2 * Math.PI;

            stateAngleDegree = Drawing.round(stateAngle * 180 / Math.PI);

            //calculo do ponto de incio transiçao
            startArc.X = origin.position.X + Math.Cos(startArcAngle) * Constants.STATE_RADIUS;
            startArc.Y = origin.position.Y + Math.Sin(startArcAngle) * Constants.STATE_RADIUS;

            //calculo do ponto de destino transição
            arcDestination.X = origin.position.X + statesDistance.Length - Math.Cos(startArcAngle) * Constants.STATE_RADIUS;
            arcDestination.Y = startArc.Y;

            //calculo primeiro ponto do arco
            firstArcPoint.X = origin.position.X + statesDistance.Length / 3;
            firstArcPoint.Y = origin.position.Y + statesDistance.Length / 5;

            //calculo segundo ponto do arco
            secondArcPoint.X = origin.position.X + 2 * statesDistance.Length / 3;
            secondArcPoint.Y = origin.position.Y + statesDistance.Length / 5;

            //calculo posião seta
            distanceArrowDestination = arcDestination - origin.position;
            arrowAnglePointFix = Math.Atan2(distanceArrowDestination.Y, distanceArrowDestination.X);
            arrowPoint.X = origin.position.X + Math.Cos(arrowAnglePointFix + stateAngle) * distanceArrowDestination.Length;
            arrowPoint.Y = origin.position.Y + Math.Sin(arrowAnglePointFix + stateAngle) * distanceArrowDestination.Length;
            arrowAngleFix = Math.Atan2(firstArcPoint.Y - startArc.Y, firstArcPoint.X - startArc.X) * 0.75;
            arrowInclination = stateAngle + Math.PI - arrowAngleFix;

            //arredondamento
            string x1 = Drawing.round(origin.position.X);
            string y1 = Drawing.round(origin.position.Y);
            string x2 = Drawing.round(startArc.X);
            string y2 = Drawing.round(startArc.Y);
            string x3 = Drawing.round(firstArcPoint.X);
            string y3 = Drawing.round(firstArcPoint.Y);
            string x4 = Drawing.round(secondArcPoint.X);
            string y4 = Drawing.round(secondArcPoint.Y);
            string x5 = Drawing.round(arcDestination.X);
            string y5 = Drawing.round(arcDestination.Y);

            file.WriteLine("\\draw[line width=0.8pt, rotate around={" + stateAngleDegree + ":(" + x1 + "," + y1 + 
                ")}, line width=.5pt, smooth] (" + x2 + "," + y2 +  ") .. controls (" + x3 + "," + y3 + ") and (" +
                x4 + "," + y4 + ") .. (" + x5 + "," + y5 + ");");

            //COLCOCAR SETA NA TRANSICAO
            drawLatexArrow(file, arrowPoint, arrowInclination, arrowPoint);

            // COLOCAR A CODIGO PARA INSERIR NOME NAS TRANSIÇOES
            double textAngleFix = Math.Atan2(statesDistance.Length / 4, statesDistance.Length / 2);
            
            transitionNamePoint.X = origin.position.X + Math.Cos(stateAngle + textAngleFix) * statesDistance.Length / 2;
            transitionNamePoint.Y = origin.position.Y + Math.Sin(stateAngle + textAngleFix) * statesDistance.Length / 2;
            transitionNamePoint = Drawing.RoundVector(transitionNamePoint);

            writeTextLatex(file, transitionNamePoint, transitionName, fontSize, stateAngle);
        }

        public static void drawAutoTransition(StreamWriter file, Vector stateCoord, double angle, string statesName, string fontFill, int fontSize)  //angulo em radianos
        {
            double gapValue = 6;
            double theta;
            Vector arrowPosition = new Vector();
            Vector point = new Vector();

            point.X = Math.Round(stateCoord.X + Math.Cos(angle) * (Constants.STATE_RADIUS + 5));
            point.Y = Math.Round(stateCoord.Y - Math.Sin(angle) * (Constants.STATE_RADIUS + 5));

            string x1 = Drawing.round(point.X + gapValue);
            string y1 = Drawing.round(point.Y - gapValue);
            string x2 = Drawing.round(point.X - 10 * gapValue);
            string y2 = Drawing.round(point.Y - 2 * gapValue);
            string x3 = Drawing.round(point.X - 10 * gapValue);
            string y3 = Drawing.round(point.Y + 2 * gapValue);
            string y4 = Drawing.round(point.Y + gapValue);
            string x5 = Drawing.round(point.X);
            string y5 = Drawing.round(point.Y);

            theta = -(angle - Math.PI);
            string thetaDegree = Drawing.round((theta * 180) / Math.PI);


            file.WriteLine("\t<path d=\"M" + x1 + " " + y1 + " " + "C" + " " + x2 + " " + y2 + ", " + x3 + " " + y3
                + ", " + x5 + " " + y4 + "\" stroke=\"black\" fill=\"transparent\"  transform=\"rotate(" + 
                thetaDegree + " " + x5 + ", " + y5 + ")\" />");

            arrowPosition.X = point.X + Math.Cos(3 * Math.PI / 2 - theta) * gapValue;
            arrowPosition.Y = point.Y - Math.Sin(3 * Math.PI / 2 - theta) * gapValue;
            arrowPosition = Drawing.RoundVector(arrowPosition);

            drawSVGArrow(file, arrowPosition, -theta + Math.PI, arrowPosition);

            // localização do local do texto
            Vector textDistance = new Vector();
            Vector transitionNamePoint = new Vector();
            Vector gap = new Vector();

            angle = angle % (2 * Math.PI);
            if ((angle > Math.PI / 2) && (angle < 3 * Math.PI / 2))
            {
                textDistance.X = Math.Sin(angle) * (Constants.TEXT_OFFSET + 7);
                textDistance.Y = Math.Cos(angle) * (Constants.TEXT_OFFSET + 7);
            }

            if ((angle >= 0 && angle <= Math.PI / 2) || (angle >= 3 * Math.PI / 2 && angle < 2 * Math.PI))
            {
                textDistance.X = -Math.Sin(angle) * (Constants.TEXT_OFFSET + 7);
                textDistance.Y = -Math.Cos(angle) * (Constants.TEXT_OFFSET + 7);
            }

            transitionNamePoint.X = stateCoord.X + 2.5 * Math.Cos(angle) * Constants.STATE_RADIUS + textDistance.X;
            transitionNamePoint.Y = stateCoord.Y - 2.5 * Math.Sin(angle) * Constants.STATE_RADIUS + textDistance.Y;
            transitionNamePoint = Drawing.RoundVector(transitionNamePoint);

            writeTextSVG(file, transitionNamePoint, statesName, angle, gap, fontFill, fontSize);
        }

        public static void drawAutoTransitionLatex(StreamWriter file, Vector stateCoord, double angle, string eventsName)  //angulo em radianos
        {
            //angle *= -1;
            double startArcAngle = Math.PI / 15;
            double arrowAngleFix, textAngleFix;
            Vector transitionNamePoint = new Vector();
            Vector parameterRef = new Vector(50, 10);
            Vector startAutoTransition = new Vector();
            Vector endAutoTransition = new Vector();
            Vector arrowPoint = new Vector();
            Vector firstRefAutoTransition = new Vector();
            Vector secondRefAutoTransition = new Vector();
            string angleDegree = Drawing.round(angle * 180 / Math.PI);

            // Calculo ponto incio Arco
            startAutoTransition.X = stateCoord.X + Math.Cos(startArcAngle) * Constants.STATE_RADIUS;
            startAutoTransition.Y = stateCoord.Y - Math.Sin(startArcAngle) * Constants.STATE_RADIUS;

            // Calculo ponto destino Arco
            endAutoTransition.X = startAutoTransition.X;
            endAutoTransition.Y = stateCoord.Y + Math.Sin(startArcAngle) * Constants.STATE_RADIUS;

            // Calculo ponto primeira referencia Arco
            firstRefAutoTransition.X = startAutoTransition.X + parameterRef.X;
            firstRefAutoTransition.Y = startAutoTransition.Y - parameterRef.Y;

            // Calculo ponto primeira referencia Arco
            secondRefAutoTransition.X = startAutoTransition.X + parameterRef.X;
            secondRefAutoTransition.Y = endAutoTransition.Y + parameterRef.Y;

            //arredondamentos
            string x1 = Drawing.round(stateCoord.X);
            string y1 = Drawing.round(stateCoord.Y);
            string x2 = Drawing.round(startAutoTransition.X);
            string y2 = Drawing.round(startAutoTransition.Y);
            string x3 = Drawing.round(firstRefAutoTransition.X);
            string y3 = Drawing.round(firstRefAutoTransition.Y);
            string x4 = Drawing.round(secondRefAutoTransition.X);
            string y4 = Drawing.round(secondRefAutoTransition.Y);
            string x5 = Drawing.round(endAutoTransition.X);
            string y5 = Drawing.round(endAutoTransition.Y);

            // gera código para desenho em latex

            file.WriteLine("\\draw[ line width= 0.8pt, rotate around={" + angleDegree + ":(" + x1 + "," + y1 + 
                ")}, line width=.5pt, smooth] (" + x2 + "," + y2 + ") .. controls (" + x3 + "," + y3 + ") and (" + 
                x4 + "," + y4 + ") .. (" + x5 + "," + y5 + ");");

            //correçao angulo seta
            arrowAngleFix = Math.Atan2(parameterRef.Y, parameterRef.X + Constants.STATE_RADIUS);

            //caculo ponto local seta
            arrowPoint.X = stateCoord.X + Math.Cos(angle + startArcAngle) * Constants.STATE_RADIUS;
            arrowPoint.Y = stateCoord.Y + Math.Sin(angle + startArcAngle) * Constants.STATE_RADIUS;

            drawLatexArrow(file, arrowPoint, angle + arrowAngleFix, arrowPoint);

            Vector textDistance = new Vector();

            if ((angle > Math.PI / 2) && (angle < 3 * Math.PI / 2))
            {
                textDistance.X = Math.Sin(angle) * (Constants.TEXT_OFFSET + 7);
                textDistance.Y = Math.Cos(angle) * (Constants.TEXT_OFFSET + 7);
            }

            if ((angle >= 0 && angle <= Math.PI / 2) || (angle >= 3 * Math.PI / 2 && angle < 2 * Math.PI))
            {
                textDistance.X = -Math.Sin(angle) * (Constants.TEXT_OFFSET + 7);
                textDistance.Y = -Math.Cos(angle) * (Constants.TEXT_OFFSET + 7);
            }

            //calculo local nome transição
            textAngleFix = Math.Atan2(parameterRef.Y + 10, parameterRef.X + Constants.STATE_RADIUS - 10);
            transitionNamePoint.X = stateCoord.X + 2.5 * Math.Cos(angle) * Constants.STATE_RADIUS + textDistance.X;
            transitionNamePoint.Y = stateCoord.Y + 2.5 * Math.Sin(angle) * Constants.STATE_RADIUS - textDistance.Y;

            writeTextLatex(file, transitionNamePoint, eventsName, "normalsize", angle);
        }

        public static void writeTextSVG(StreamWriter file, Vector position, string text, double thetaInRad, Vector gap, string fontFill, int fontSize)
        {
            //Vector pontoRound = new Vector(Math.Round(ponto.X), Math.Round(ponto.Y));
            double thetaDegree = (180 * thetaInRad) / Math.PI;
            //tetaGrau = Math.Round(tetaGrau);

            if (thetaDegree < 0) thetaDegree += 360;

            if (thetaDegree > 90 && thetaDegree <= 180) thetaDegree += 180;

            else if (thetaDegree > 180 && thetaDegree < 270) thetaDegree = (thetaDegree + 180) % 360;

            string x1 = Drawing.round(position.X);
            string y1 = Drawing.round(position.Y);
            string x2 = Drawing.round(gap.X);
            string y2 = Drawing.round(gap.Y);
            string theta = Drawing.round(thetaDegree);

            file.WriteLine("\t<text x=\"" + x1 + "\" y=\"" + y1 + "\" dx=\"" + x2 + "\" dy=\"" + y2 + 
                "\" font-size=\"" + fontSize + "\" fill=\"black\" stroke=\"" + fontFill + 
                "\" text-anchor=\"middle\" transform=\"rotate(-" + theta + " " + x1 + "," + y1 + ")\">" + 
                text + "</text>");
        }

        public static void writeTextLatex(StreamWriter file, Vector position, string text, string fontSize, double thetaInRad)
        {
            if (thetaInRad<0) thetaInRad += 2 * Math.PI;

            thetaInRad %= (2 * Math.PI);
            //Vector pontoRound = new Vector(Math.Round(ponto.X), Math.Round(ponto.Y));
            double thetaDegree = (180 * thetaInRad) / Math.PI;

            if (thetaDegree < 0) thetaDegree += 360;

            if (thetaDegree > 90 && thetaDegree <= 180) thetaDegree += 180;

            else if (thetaDegree > 180 && thetaDegree < 270) thetaDegree = (thetaDegree + 180) % 360;

            string x1 = Drawing.round(position.X);
            string y1 = Drawing.round(position.Y);
            string theta = Drawing.round(thetaDegree);

            file.WriteLine("\\draw (" + x1 + "," + y1 + ") node [rotate=" + theta + "] {\\" + fontSize + " $" + 
                text + "$};");
        }

        public static void drawFigureSVG(StreamWriter file, Dictionary<string, DrawingState> statesList)
        {
            Vector length = new Vector();
            Vector arrowOrigin = new Vector();
            Vector arrowDestination = new Vector();

            foreach (var item in statesList)
            {
                foreach (var element in item.Value.destinationStates)
                {
                    string eventsNames = element.Value.Item1;

                    if (item.Value.Alias != element.Key.Alias)
                    {
                        drawCurveTransition2(file, item.Value, element.Key, eventsNames, "none", 18);
                    }
                    else
                    {
                        //calcula posiçao para inserir a auto transição
                        List<double> angles = new List<double>();

                        double gap = Math.PI / 4;
                        double stateAngle;

                        foreach (var destinationStatePair in item.Value.destinationStates)
                        {
                            length = destinationStatePair.Key.position - item.Value.position;
                            stateAngle = Math.Atan2(-length.Y, length.X);

                            if (stateAngle < 0) stateAngle += 2 * Math.PI;

                            if (!angles.Contains(stateAngle) && !stateAngle.Equals(0))
                            {
                                angles.Add(stateAngle);
                            }
                        }
                        foreach (var originStatePair in item.Value.originStates)
                        {
                            length = originStatePair.Key.position - item.Value.position;
                            stateAngle = Math.Atan2(-length.Y, length.X);

                            if (stateAngle < 0) stateAngle += 2 * Math.PI;

                            if (!angles.Contains(stateAngle) && !stateAngle.Equals(0))
                            {
                                angles.Add(stateAngle);
                            }
                        }

                        angles.Sort();                     //organiza os Vector de angulos em ordem crescente

                        if (item.Value.initialState) gap = 3 * Math.PI / 4;
                        else
                        {
                            if (angles.Count() > 1)
                            {
                                int iterador = angles.Count() - 1;
                                if (angles[iterador] - angles[iterador - 1] > 2 * Math.PI - angles[iterador] + angles[0])
                                {
                                    gap = ((angles[iterador] - angles[iterador - 1]) / 2 + angles[iterador - 1]) % (2 * Math.PI);
                                }
                                else
                                {
                                    gap = ((2 * Math.PI - angles[iterador] + angles[0]) / 2 + angles[iterador]) % (2 * Math.PI);
                                }

                            }
                            else
                                gap = angles[0] + Math.PI;
                        }
                        drawAutoTransition(file, item.Value.position, gap, eventsNames, "none", 18);
                    }

                    if (item.Value.initialState)
                    {
                        arrowOrigin.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE + 50);
                        arrowOrigin.Y = item.Value.position.Y;

                        arrowDestination.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE);
                        arrowDestination.Y = item.Value.position.Y;

                        Vector reference = new Vector();

                        drawSVGLine(file, arrowOrigin, arrowDestination, 0, reference);
                        drawSVGArrow(file, arrowDestination, Math.PI, arrowDestination);
                    }
                }
            }
        }

        public static void drawFigureLatex(StreamWriter file, Dictionary<string, DrawingState> statesList, string fontSize)
        {
            Vector length = new Vector();
            Vector arrowOrigin = new Vector();
            Vector arrowDestination = new Vector();

            foreach (var item in statesList)
            {
                foreach (var element in item.Value.destinationStates)
                {
                    //string nomeEventos = Geral.GeraStringTransicoes(elemento.estado, item.Value.estadosDestino);
                    string eventsName = element.Value.Item1;

                    if (item.Value.Alias != element.Key.Alias)
                    {
                        drawCurveTransitionLatex(file, item.Value, element.Key, eventsName, fontSize);
                    }
                    else
                    {
                        //calcula posiçao para inserir a auto transição

                        List<double> angles = new List<double>();

                        double gap = Math.PI / 4;
                        double stateAngle;

                        foreach (var destinationStatePair in item.Value.destinationStates)
                        {
                            length = destinationStatePair.Key.position - item.Value.position;
                            stateAngle = Math.Atan2(-length.Y, length.X);

                            if (stateAngle < 0) stateAngle += 2 * Math.PI;

                            if (!angles.Contains(stateAngle) && !stateAngle.Equals(0)) angles.Add(stateAngle);
                        }
                        foreach (var originStatePair in item.Value.originStates)
                        {
                            length = originStatePair.Key.position - item.Value.position;
                            stateAngle = Math.Atan2(-length.Y, length.X);

                            if (stateAngle < 0) stateAngle += 2 * Math.PI;

                            if (!angles.Contains(stateAngle) && !stateAngle.Equals(0)) angles.Add(stateAngle);
                        }

                        angles.Sort();                     //organiza os Vector de angulos em ordem crescente

                        if (item.Value.initialState) gap = 3 * Math.PI / 4;
                        else
                        {
                            if (angles.Count() > 1)
                            {
                                int iterador = angles.Count() - 1;
                                if (angles[iterador] - angles[iterador - 1] > 2 * Math.PI - angles[iterador] + angles[0])
                                {
                                    gap = ((angles[iterador] - angles[iterador - 1]) / 2 + angles[iterador - 1]) % (2 * Math.PI);
                                }
                                else
                                {
                                    gap = ((2 * Math.PI - angles[iterador] + angles[0]) / 2 + angles[iterador]) % (2 * Math.PI);
                                }

                            }
                            else gap = angles[0] + Math.PI;
                        }
                        drawAutoTransitionLatex(file, item.Value.position, gap, eventsName);
                    }

                    if (item.Value.initialState)
                    {
                        arrowOrigin.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE + 50);
                        arrowOrigin.Y = item.Value.position.Y;

                        arrowDestination.X = item.Value.position.X - (Constants.STATE_RADIUS + Constants.DISTANCE);
                        arrowDestination.Y = item.Value.position.Y;

                        Vector reference = new Vector();

                        drawLatexLine(file, arrowOrigin, arrowDestination, 0, reference);
                        drawLatexArrow(file, arrowDestination, Math.PI, arrowDestination);
                    }
                }
            }
        }
    }
}
