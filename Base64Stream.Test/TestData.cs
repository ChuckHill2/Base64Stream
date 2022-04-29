using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base64Stream.Test
{
    public class TestData
    {
        public const string TestString = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque ultrices enim eu velit efficitur vestibulum. Aenean dictum, erat non luctus fermentum, tortor neque egestas nisl, vitae tristique erat diam vitae ligula. Vestibulum tincidunt sapien faucibus quam dapibus congue. Curabitur bibendum dapibus faucibus. Fusce hendrerit elit dolor, vitae tempor massa tristique in. Praesent quis ante hendrerit, blandit metus nec, aliquet ligula. Nunc tincidunt justo id tincidunt elementum.
In hac habitasse platea dictumst. Sed vulputate rutrum dui. Maecenas placerat sodales finibus. Vivamus eu quam massa. Suspendisse ac malesuada enim. Quisque venenatis, ex quis posuere sodales, purus lorem tincidunt dolor, eget ultricies est arcu porta nisi. Vestibulum vestibulum cursus rhoncus. Ut pharetra est erat, nec consectetur elit suscipit quis. Vestibulum dictum lorem facilisis risus facilisis volutpat. Proin leo diam, maximus at purus at, ullamcorper dignissim risus. Donec quis facilisis urna, quis pellentesque mi. Proin ipsum enim, pellentesque at aliquam in, convallis a orci. Vivamus vel quam tincidunt, placerat nulla non, luctus metus. Suspendisse potenti. Praesent accumsan neque in odio lobortis tempor.
Proin et sem et urna luctus consectetur ac in magna. Mauris varius consectetur sapien, quis semper nisl cursus at. Cras at eleifend eros. Vestibulum sed molestie dui, a imperdiet nisi. Maecenas ac elit ut enim scelerisque vehicula id tincidunt eros. Nulla augue tortor, efficitur ac pretium sed, consectetur in nisl. Praesent arcu nibh, eleifend ac auctor sit amet, cursus eget risus. Integer in blandit enim, sed vehicula massa.
In cursus lectus eros, cursus auctor nulla pellentesque at. Praesent in sollicitudin ante. Sed finibus posuere erat, a lacinia arcu pellentesque a. Sed nec laoreet orci, quis fermentum metus. Nulla maximus, turpis vel ullamcorper facilisis, velit augue efficitur ligula, eget venenatis sem ante ac nunc. Interdum et malesuada fames ac ante ipsum primis in faucibus. Aenean porttitor vulputate libero eget malesuada. Maecenas elementum interdum risus, id aliquam purus semper id. Cras porta, nisi a ultricies tincidunt, risus enim imperdiet orci, eu sodales ante mi id velit. Proin non tempor leo. Aenean ut blandit quam. Nullam vitae turpis ut nunc sagittis vehicula. Nunc lobortis, velit elementum sagittis lacinia, risus dui dapibus felis, mollis viverra ligula leo sed mi. Donec at hendrerit magna. Praesent ornare libero vel turpis ornare luctus.
Nunc metus orci, luctus fermentum hendrerit non, aliquam sit amet massa. Aliquam nec laoreet enim. Ut eget lacus ut sapien vestibulum aliquet at et nibh. Nam egestas eu libero nec iaculis. Maecenas sagittis mi tortor, vel malesuada est aliquam ac. Suspendisse viverra velit erat, sed pretium augue consectetur nec. Vestibulum a ante ac velit ullamcorper pellentesque iaculis vestibulum purus. In hac habitasse platea dictumst. Donec quis dignissim est. Sed sed sapien non metus feugiat scelerisque id non justo. Nulla vitae sodales velit. Fusce eu quam id ligula condimentum mollis. Mauris quis lectus elementum nibh tempor ultricies vel tristique metus. Quisque iaculis sollicitudin enim, eget bibendum elit fermentum id.
Aliquam semper orci vel luctus interdum. Nunc eleifend non nulla in finibus. Integer a vulputate nisl. Integer neque neque, vehicula vel diam at, fringilla molestie arcu. Nunc ut malesuada mi. Sed ornare rutrum tortor quis cursus. Aliquam sollicitudin tempor venenatis. Sed in massa tincidunt, dignissim nulla eget, placerat nisi. Pellentesque rutrum nunc vitae ultrices vestibulum. Curabitur tristique vulputate eleifend. Maecenas egestas quam at tellus lobortis, a pharetra dui molestie. Nunc dignissim condimentum quam a pretium. Praesent fringilla lectus vel dui varius interdum.
Mauris commodo libero at tellus tincidunt fringilla. Nulla sapien orci, aliquam ac lectus et, vehicula molestie tellus. Suspendisse elementum blandit felis id rutrum. Maecenas blandit feugiat ligula a commodo. Sed in porttitor nisl, in maximus ligula. Aliquam ante sapien, pretium id facilisis nec augue.";

        public static readonly byte[] TestBytes = Encoding.UTF8.GetBytes(TestString);

        public static readonly string TestBase64String = System.Convert.ToBase64String(TestBytes);

        public static readonly string TestBase64StringWithLineBreaks = System.Convert.ToBase64String(TestBytes, Base64FormattingOptions.InsertLineBreaks);

        public static readonly string ProjectDir = GetProjectDir();
        public static readonly string TestLargeImage = ProjectDir + "TestLarge.jpg";
        public static readonly string TestMediumImage = ProjectDir + "TestMedium.jpg";
        public static readonly string TestSmallImage = ProjectDir + "TestSmall.png";
        public static string GetProjectDir()
        {
            var exe = Process.GetCurrentProcess().MainModule.FileName;
            var filename = Path.GetFileNameWithoutExtension(exe);
            var dir = Path.GetDirectoryName(exe);
            var contains = "\\" + filename + "\\";
            int i = dir.LastIndexOf(contains);
            if (i == -1) return dir + "\\";
            var s = dir.Substring(0, i + contains.Length);
            return s;
        }
    }
}
